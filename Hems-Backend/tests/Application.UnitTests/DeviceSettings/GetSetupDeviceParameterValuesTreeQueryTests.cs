using FluentAssertions;
using Hems_Backend.Application.DeviceSettings.Queries.GetSetupDeviceParameterValuesTree;
using NUnit.Framework;

namespace Hems_Backend.Application.UnitTests.DeviceSettings;

[TestFixture]
public class GetSetupDeviceParameterValuesTreeQueryTests
{
    [Test]
    public void PruneEmptyGroups_ShouldKeepGroupsWithParameters()
    {
        // Arrange
        var groups = new List<ParameterGroupTreeDto>
        {
            new ParameterGroupTreeDto
            {
                Id = Guid.NewGuid(),
                Name = "Battery Settings",
                Parameters = new List<ParameterTreeDto>
                {
                    new ParameterTreeDto { Id = Guid.NewGuid(), Name = "BatteryCapacity" }
                },
                Children = new List<ParameterGroupTreeDto>()
            }
        };

        // Act
        var result = GetSetupDeviceParameterValuesTreeQueryHandler.PruneEmptyGroups(groups);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Battery Settings");
    }

    [Test]
    public void PruneEmptyGroups_ShouldRemoveGroupsWithNoParametersAndNoChildren()
    {
        // Arrange - simulates a "Grid Standard" group that had only grid config params (now filtered out)
        var groups = new List<ParameterGroupTreeDto>
        {
            new ParameterGroupTreeDto
            {
                Id = Guid.NewGuid(),
                Name = "Grid Standard",
                Parameters = new List<ParameterTreeDto>(),
                Children = new List<ParameterGroupTreeDto>()
            },
            new ParameterGroupTreeDto
            {
                Id = Guid.NewGuid(),
                Name = "Battery Settings",
                Parameters = new List<ParameterTreeDto>
                {
                    new ParameterTreeDto { Id = Guid.NewGuid(), Name = "BatteryCapacity" }
                },
                Children = new List<ParameterGroupTreeDto>()
            }
        };

        // Act
        var result = GetSetupDeviceParameterValuesTreeQueryHandler.PruneEmptyGroups(groups);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Battery Settings");
    }

    [Test]
    public void PruneEmptyGroups_ShouldKeepParentGroupIfChildHasParameters()
    {
        // Arrange - parent has no direct params but child group has params
        var groups = new List<ParameterGroupTreeDto>
        {
            new ParameterGroupTreeDto
            {
                Id = Guid.NewGuid(),
                Name = "System Settings",
                Parameters = new List<ParameterTreeDto>(),
                Children = new List<ParameterGroupTreeDto>
                {
                    new ParameterGroupTreeDto
                    {
                        Id = Guid.NewGuid(),
                        Name = "Battery Settings",
                        Parameters = new List<ParameterTreeDto>
                        {
                            new ParameterTreeDto { Id = Guid.NewGuid(), Name = "BatteryCapacity" }
                        },
                        Children = new List<ParameterGroupTreeDto>()
                    }
                }
            }
        };

        // Act
        var result = GetSetupDeviceParameterValuesTreeQueryHandler.PruneEmptyGroups(groups);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("System Settings");
        result[0].Children.Should().HaveCount(1);
        result[0].Children[0].Name.Should().Be("Battery Settings");
    }

    [Test]
    public void PruneEmptyGroups_ShouldRemoveNestedEmptyGroups()
    {
        // Arrange - parent has child that is now empty after grid config filtering
        var groups = new List<ParameterGroupTreeDto>
        {
            new ParameterGroupTreeDto
            {
                Id = Guid.NewGuid(),
                Name = "Device Configuration",
                Parameters = new List<ParameterTreeDto>(),
                Children = new List<ParameterGroupTreeDto>
                {
                    new ParameterGroupTreeDto
                    {
                        Id = Guid.NewGuid(),
                        Name = "Grid Standard",
                        Parameters = new List<ParameterTreeDto>(),
                        Children = new List<ParameterGroupTreeDto>()
                    }
                }
            }
        };

        // Act
        var result = GetSetupDeviceParameterValuesTreeQueryHandler.PruneEmptyGroups(groups);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void PruneEmptyGroups_ShouldHandleEmptyInput()
    {
        // Arrange
        var groups = new List<ParameterGroupTreeDto>();

        // Act
        var result = GetSetupDeviceParameterValuesTreeQueryHandler.PruneEmptyGroups(groups);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void PruneEmptyGroups_ShouldKeepMixedParentWithSomeEmptyChildren()
    {
        // Arrange - parent with one empty child (Grid Standard) and one child with params (Battery)
        var groups = new List<ParameterGroupTreeDto>
        {
            new ParameterGroupTreeDto
            {
                Id = Guid.NewGuid(),
                Name = "Device Configuration",
                Parameters = new List<ParameterTreeDto>(),
                Children = new List<ParameterGroupTreeDto>
                {
                    new ParameterGroupTreeDto
                    {
                        Id = Guid.NewGuid(),
                        Name = "Grid Standard",
                        Parameters = new List<ParameterTreeDto>(),
                        Children = new List<ParameterGroupTreeDto>()
                    },
                    new ParameterGroupTreeDto
                    {
                        Id = Guid.NewGuid(),
                        Name = "Battery Settings",
                        Parameters = new List<ParameterTreeDto>
                        {
                            new ParameterTreeDto { Id = Guid.NewGuid(), Name = "BatteryCapacity" }
                        },
                        Children = new List<ParameterGroupTreeDto>()
                    }
                }
            }
        };

        // Act
        var result = GetSetupDeviceParameterValuesTreeQueryHandler.PruneEmptyGroups(groups);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Device Configuration");
        result[0].Children.Should().HaveCount(1);
        result[0].Children[0].Name.Should().Be("Battery Settings");
    }
}
