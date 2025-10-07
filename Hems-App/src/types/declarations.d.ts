declare module 'react-native-charts-wrapper' {
  import { Component } from 'react';
  import { ViewProps } from 'react-native';

  export interface CombinedChartProps extends ViewProps {
    style?: any;
    data?: any;
    xAxis?: any;
    yAxis?: any;
    legend?: any;
    marker?: any;
    chartDescription?: any;
    drawOrder?: string[];
    touchEnabled?: boolean;
    dragEnabled?: boolean;
    scaleEnabled?: boolean;
    pinchZoom?: boolean;
    doubleTapToZoomEnabled?: boolean;
    highlightPerTapEnabled?: boolean;
    highlightPerDragEnabled?: boolean;
    animation?: {
      durationX?: number;
      durationY?: number;
      easingX?: string;
      easingY?: string;
    };
    viewPortOffsets?: {
      left?: number;
      top?: number;
      right?: number;
      bottom?: number;
    };
    extraOffsets?: {
      left?: number;
      top?: number;
      right?: number;
      bottom?: number;
    };
    highlightLineWidth?: number;
    backgroundColor?: string;
    gridBackgroundColor?: string;
    drawGridBackground?: boolean;
  }

  export class CombinedChart extends Component<CombinedChartProps> {}
}
