using System.Collections.Generic;
using SalmonEgg.Domain.Models.Plan;
using SalmonEgg.Domain.Models.Protocol;
using SalmonEgg.Domain.Services;
using SalmonEgg.Presentation.Core.Services.Chat;
using Xunit;

namespace SalmonEgg.Presentation.Core.Tests.Chat;

public class AcpSessionUpdateProjectorTests
{
    [Fact]
    public void ProjectSessionNew_MapsModesAndConfigOptions()
    {
        var projector = new AcpSessionUpdateProjector();
        var response = new SessionNewResponse(
            sessionId: "remote-1",
            modes: new SessionModesState
            {
                CurrentModeId = "agent",
                AvailableModes = new List<SessionMode>
                {
                    new() { Id = "agent", Name = "Agent", Description = "Agent mode" },
                    new() { Id = "plan", Name = "Plan", Description = "Plan mode" }
                }
            },
            configOptions: new List<ConfigOption>
            {
                new()
                {
                    Id = "mode",
                    Category = "mode",
                    CurrentValue = "agent",
                    Options = new List<ConfigOptionValue>
                    {
                        new() { Value = "agent", Name = "Agent" },
                        new() { Value = "plan", Name = "Plan" }
                    }
                }
            });

        var delta = projector.ProjectSessionNew(response);

        Assert.Equal("agent", delta.SelectedModeId);
        Assert.Equal(2, delta.AvailableModes?.Count);
        Assert.True(delta.ShowConfigOptionsPanel);
        Assert.Single(delta.ConfigOptions!);
    }

    [Fact]
    public void Project_MapsPlanUpdateToPlanPanelProjection()
    {
        var projector = new AcpSessionUpdateProjector();
        var args = new SessionUpdateEventArgs(
            "remote-1",
            new PlanUpdate(
                entries: new List<PlanEntry>
                {
                    new() { Content = "Step 1", Status = PlanEntryStatus.Pending, Priority = PlanEntryPriority.High }
                },
                title: "My plan"));

        var delta = projector.Project(args);

        Assert.True(delta.ShowPlanPanel);
        Assert.Equal("My plan", delta.PlanTitle);
        Assert.Single(delta.PlanEntries!);
    }
}
