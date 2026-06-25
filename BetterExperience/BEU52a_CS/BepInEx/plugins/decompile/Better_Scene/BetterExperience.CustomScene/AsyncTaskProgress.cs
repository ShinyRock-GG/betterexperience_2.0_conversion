using BetterExperience.GameScopes;

namespace BetterExperience.CustomScene;

public class AsyncTaskProgress : ObservableValue<(float, int, int)>
{
	public AsyncTaskProgress(float percent = 0f, int finishedCount = 0, int totalCount = 0)
		: base((percent, finishedCount, totalCount))
	{
	}

	internal void Report(int item, int count)
	{
		if (count > 0)
		{
			base.Value = (100f * (float)item / (float)count, item, count);
		}
		else
		{
			base.Value = (0f, 0, 0);
		}
	}
}
