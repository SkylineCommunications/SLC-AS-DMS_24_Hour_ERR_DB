namespace DMA_24_Hour_ERR_DB_1
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Skyline.DataMiner.Automation;
    using Skyline.DataMiner.Core.DataMinerSystem.Automation;
    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.Net.Filters;
    using Skyline.DataMiner.Net.Messages;

    /// <summary>
    /// Represents a DataMiner Automation script.
    /// </summary>
    public class Script
    {
        /// <summary>
        /// The script entry point.
        /// </summary>
        /// <param name="engine">Link with SLAutomation process.</param>
		public void Run(IEngine engine)
		{
			try
			{
				RunSafe(engine);
			}
			catch (ScriptAbortException)
			{
				// Catch normal abort exceptions (engine.ExitFail or engine.ExitSuccess)
				throw; // Comment if it should be treated as a normal exit of the script.
			}
			catch (ScriptForceAbortException)
			{
				// Catch forced abort exceptions, caused via external maintenance messages.
				throw;
			}
			catch (ScriptTimeoutException)
			{
				// Catch timeout exceptions for when a script has been running for too long.
				throw;
			}
			catch (InteractiveUserDetachedException)
			{
				// Catch a user detaching from the interactive script by closing the window.
				// Only applicable for interactive scripts, can be removed for non-interactive scripts.
				throw;
			}
			catch (Exception e)
			{
				engine.ExitFail("Run|Something went wrong: " + e);
			}
		}

		private void RunSafe(IEngine engine)
		{
            List<TestResult> results = new List<TestResult>();

            IDms dms = engine.GetDms();
            var agentNames = dms.GetAgents().Select(x => new Tuple<string, int>(x.Name, x.Id));

            var alarmFilterItem = new AlarmFilterItemInt
            {
                CompareType = AlarmFilterCompareType.Equality,
                Field = AlarmFilterField.SeverityID,
                Values = new[] { 24 },
            };

            var alarmFilter = new AlarmFilter { FilterItems = new AlarmFilterItem[] { alarmFilterItem } };

            var endTime = DateTime.Now;
            DMSMessage[] responses = engine.SendSLNetMessage(new GetAlarmDetailsFromDbMessage
            {
                StartTime = endTime - new TimeSpan(24, 0, 0),
                EndTime = endTime,
                AlarmTable = true,
                Filter = alarmFilter,
            });

            var alarmMessages = responses.Select(x => x as AlarmEventMessage);

            foreach (var agent in agentNames)
            {
                TestResult testResult = new TestResult
                {
                    ParameterName = "24 Hour ERR DBs",
                    DmaName = agent.Item1,
                    ReceivedValue = Convert.ToString(alarmMessages.Count(x => agent.Item2.Equals(x.HostingAgentID) && x.ParameterName == "Database")),
                };

                results.Add(testResult);
            }

            engine.AddScriptOutput("result", JsonConvert.SerializeObject(results));
        }
	}

    public class TestResult
    {
        public string ParameterName { get; set; }

        public string DisplayName { get; set; }

        public string ElementName { get; set; }

        public string DmaName { get; set; }

        public string ReceivedValue { get; set; }

        public string ExpectedValue { get; set; }

        public bool Success { get; set; }
    }
}
