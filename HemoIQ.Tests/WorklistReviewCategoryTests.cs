using HemoIQ.Models;

namespace HemoIQ.Tests;

public class WorklistReviewCategoryTests
{
    [Fact]
    public void EveryKind_BelongsToExactlyOneCard_AndTheNewKindsAreReview()
    {
        foreach (var kind in Enum.GetValues<WorklistKind>())
            Assert.Contains(LabSettings.WorklistCategory(kind), LabSettings.WorklistCategories);
        Assert.Equal("Review", LabSettings.WorklistCategory(WorklistKind.OverdueAdditionalTesting));
        Assert.Equal("Review", LabSettings.WorklistCategory(WorklistKind.RuleConflict));
        Assert.Equal("Review", LabSettings.WorklistCategory(WorklistKind.AwaitingSupervisorReview));
        Assert.Equal("Incomplete", LabSettings.WorklistCategory(WorklistKind.IncompleteReactions));
        Assert.Equal("Expired", LabSettings.WorklistCategory(WorklistKind.ExpiredPanel));
    }

    [Fact]
    public void IsolateReview_HidesOtherCards_AndClickingAgainRestoresAll()
    {
        var s = LabSettings.CreateDefault();
        Assert.True(s.ShowsAllWorklistCategories);

        s.IsolateOrRestoreWorklistCategory("Review");
        Assert.True(s.IsWorklistCategoryIsolated("Review"));
        Assert.True(s.ShowsWorklistKind(WorklistKind.RuleConflict));
        Assert.False(s.ShowsWorklistKind(WorklistKind.IncompleteReactions));
        Assert.Equal("Showing only items to review.", s.IsolatedWorklistLabel);

        s.IsolateOrRestoreWorklistCategory("Review");
        Assert.True(s.ShowsAllWorklistCategories);
        Assert.Equal("", s.IsolatedWorklistLabel);
    }

    [Fact]
    public void IsolatingAnotherCard_NowHidesReviewItems_AndUnknownCategoryIsIgnored()
    {
        var s = LabSettings.CreateDefault();
        s.IsolateOrRestoreWorklistCategory("Incomplete");
        Assert.True(s.ShowsWorklistKind(WorklistKind.IncompleteReactions));
        Assert.False(s.ShowsWorklistKind(WorklistKind.OverdueAdditionalTesting));

        s.IsolateOrRestoreWorklistCategory("Bogus");
        Assert.True(s.IsWorklistCategoryIsolated("Incomplete"));

        s.WorklistShowReview = false;
        s.SetWorklistCategory("Review", true);
        Assert.True(s.WorklistShowReview);
    }
}
