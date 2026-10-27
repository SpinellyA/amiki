using Amiki.Application.Assistant;
using Amiki.Tests.Pomodoro;

namespace Amiki.Tests.Assistant;

public class BriefingGateTests
{
    private static readonly Guid Alex = Guid.NewGuid();
    private static readonly Guid Mekai = Guid.NewGuid();

    [Fact]
    public void Hitting_the_limit_pauses_only_that_person_and_only_for_a_while()
    {
        var clock = new FakeClock();
        var gate = new BriefingGate(clock);

        gate.PauseAfterRateLimit(Alex);

        Assert.True(gate.IsPaused(Alex));
        Assert.False(gate.IsPaused(Mekai)); // everyone has their own key and limit
        clock.Advance(BriefingGate.PauseAfterLimit);
        Assert.False(gate.IsPaused(Alex));
    }

    [Fact]
    public async Task One_note_is_written_at_a_time_per_person()
    {
        var gate = new BriefingGate(TimeProvider.System);

        var first = await gate.WaitToWriteAsync(Alex, CancellationToken.None);
        var second = gate.WaitToWriteAsync(Alex, CancellationToken.None);
        var someoneElse = await gate.WaitToWriteAsync(Mekai, CancellationToken.None); // not blocked by Alex

        Assert.False(second.IsCompleted);
        first.Dispose();
        (await second).Dispose();
        someoneElse.Dispose();
    }
}
