import React, { useMemo } from 'react';
import { View } from 'react-native';
import Svg, { Line, Path, Text as SvgText, G } from 'react-native-svg';
import { scaleLinear, scaleTime } from 'd3-scale';
import { line, curveMonotoneX } from 'd3-shape';
import { LineChartProps, PowerMetric } from './PowerMetricsChart.types';
import { lineChartStyles } from './PowerMetricsChart.styles';

export const LineChart: React.FC<LineChartProps> = ({
  data,
  metrics,
  dimensions,
}) => {
  const { width, height, marginTop, marginRight, marginBottom, marginLeft } = dimensions;

  // Early return if no data
  if (!data || data.length === 0) {
    return null;
  }

  const innerWidth = width - marginLeft - marginRight;
  const innerHeight = height - marginTop - marginBottom;

  const scales = useMemo(() => {
    try {
      // X scale for time (assuming data has timestamps for 24 hours)
      let xDomain: [Date, Date];

      if (data.length > 0 && data[0].timestamp && data[data.length - 1].timestamp) {
        // Use actual data timestamps
        const startDate = new Date(data[0].timestamp);
        const endDate = new Date(data[data.length - 1].timestamp);

        // Validate dates
        if (!isNaN(startDate.getTime()) && !isNaN(endDate.getTime())) {
          xDomain = [startDate, endDate];
        } else {
          throw new Error('Invalid timestamp in data');
        }
      } else {
        // Use today's date range as default
        const today = new Date();
        const startOfDay = new Date(today);
        startOfDay.setHours(0, 0, 0, 0);
        const endOfDay = new Date(today);
        endOfDay.setHours(23, 59, 59, 999);
        xDomain = [startOfDay, endOfDay];
      }

      const xScale = scaleTime()
        .domain(xDomain)
        .range([0, innerWidth]);

      // Y scale for power values (-4 to 4 kW)
      const yScale = scaleLinear()
        .domain([-4, 4])
        .range([innerHeight, 0]);

      return { xScale, yScale };
    } catch (error) {
      // Return default scales to prevent crash
      const today = new Date();
      const startOfDay = new Date(today);
      startOfDay.setHours(0, 0, 0, 0);
      const endOfDay = new Date(today);
      endOfDay.setHours(23, 59, 59, 999);

      const xScale = scaleTime()
        .domain([startOfDay, endOfDay])
        .range([0, innerWidth]);

      const yScale = scaleLinear()
        .domain([-4, 4])
        .range([innerHeight, 0]);

      return { xScale, yScale };
    }
  }, [data, innerWidth, innerHeight]);

  const lines = useMemo(() => {
    // Return empty array if no data or invalid scales
    if (data.length === 0) {
      return [];
    }

    try {
      const { xScale, yScale } = scales;

      return metrics
        .filter(metric => metric.visible)
        .map(metric => {
          const lineGenerator = line<PowerMetric>()
            .defined(d => {
              if (!d || !d.timestamp) return false;
              const date = new Date(d.timestamp);
              return !isNaN(date.getTime()) && d[metric.key] != null;
            })
            .x(d => {
              try {
                const timestamp = new Date(d.timestamp);
                if (isNaN(timestamp.getTime())) {
                  return 0;
                }
                return xScale(timestamp);
              } catch (e) {
                return 0;
              }
            })
            .y(d => {
              const value = typeof d[metric.key] === 'number' ? d[metric.key] : 0;
              // Load values should be shown as negative
              if (metric.key === 'load') {
                return yScale(-Math.abs(value));
              }
              return yScale(value);
            })
            .curve(curveMonotoneX);

          const pathData = lineGenerator(data);

          return {
            key: metric.key,
            path: pathData || '',
            color: metric.color,
          };
        });
    } catch (error) {
      return [];
    }
  }, [data, metrics, scales]);

  const gridLines = useMemo(() => {
    const { yScale } = scales;
    const yTicks = [-4, -2, 0, 2, 4];

    return yTicks.map(tick => ({
      y: yScale(tick),
      label: `${tick}`,
    }));
  }, [scales]);

  const xAxisLabels = useMemo(() => {
    try {
      const { xScale } = scales;
      const hours = [0, 4, 8, 12, 16, 20];

      return hours.map(hour => {
        const date = new Date();
        date.setHours(hour, 0, 0, 0);
        return {
          x: xScale(date),
          label: `${hour.toString().padStart(2, '0')}:00`,
        };
      });
    } catch (error) {
      return [];
    }
  }, [scales]);

  return (
    <View style={lineChartStyles.svgContainer}>
      <Svg width={width} height={height}>
        <G transform={`translate(${marginLeft},${marginTop})`}>
          {/* Grid lines */}
          {gridLines.map((gridLine, index) => (
            <G key={`gridline-${index}`}>
              <Line
                x1={0}
                x2={innerWidth}
                y1={gridLine.y}
                y2={gridLine.y}
                stroke="rgba(255, 255, 255, 0.1)"
                strokeDasharray="2,4"
              />
              <SvgText
                x={-10}
                y={gridLine.y + 4}
                fill="#666"
                fontSize="11"
                textAnchor="end"
              >
                {gridLine.label}
              </SvgText>
            </G>
          ))}

          {/* Zero line */}
          <Line
            x1={0}
            x2={innerWidth}
            y1={scales.yScale(0)}
            y2={scales.yScale(0)}
            stroke="rgba(255, 255, 255, 0.3)"
            strokeWidth={1}
          />

          {/* Data lines */}
          {lines.map(({ key, path, color }) => (
            <Path
              key={key}
              d={path}
              stroke={color}
              strokeWidth={2}
              fill="none"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          ))}

          {/* X-axis labels */}
          {xAxisLabels.map((label, index) => (
            <SvgText
              key={`xlabel-${index}`}
              x={label.x}
              y={innerHeight + 20}
              fill="#666"
              fontSize="11"
              textAnchor="middle"
            >
              {label.label}
            </SvgText>
          ))}

          {/* Y-axis label */}
          <SvgText
            x={-marginLeft + 10}
            y={-5}
            fill="#888"
            fontSize="10"
          >
            kW
          </SvgText>
        </G>
      </Svg>
    </View>
  );
};