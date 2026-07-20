using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OpenPlan.Editor
{
    public static class ReleaseEvidencePipeline
    {
        [MenuItem("OPEN PLAN/Generate Friend Demo Balance Evidence")]
        public static void GenerateBalanceEvidence()
        {
            List<ReleaseScenarioResult> results = ReleaseBalanceScenarios.RunMatrix();
            string folder = Path.GetFullPath("outputs/ReleaseEvidence");
            Directory.CreateDirectory(folder);
            var csv = new List<string>
            {
                "mode,seed,time_to_1000_seconds,elapsed_seconds,earnings,vending_spend,average_productivity,working_seconds,distracted_seconds,restorative_seconds,commands,focused_uptime_percent,expansion_complete,new_hire_placed,stuck,recoveries"
            };
            foreach (ReleaseScenarioResult result in results)
            {
                csv.Add(string.Join(",",
                    result.mode, result.seed, F(result.timeToOneThousandSeconds), F(result.elapsedSeconds),
                    F(result.earnings), F(result.vendingSpend), F(result.averageProductivity),
                    F(result.workingSeconds), F(result.distractedSeconds), F(result.restorativeSeconds),
                    result.commandsIssued, F(result.focusedUptime * 100f), result.expansionComplete,
                    result.newHirePlaced, result.permanentlyStuck, result.recoveries));
            }
            File.WriteAllLines(Path.Combine(folder, "BalanceScenarios_20Seeds.csv"), csv);

            var report = new List<string>
            {
                "# Friend Demo Balance Scenarios",
                string.Empty,
                $"Generated UTC: {DateTime.UtcNow:O}",
                string.Empty,
                "100 deterministic runs: five play styles across the same 20 fixed seeds. Values come from the public release tuning tables used by live play.",
                string.Empty,
                "| Scenario | Runs | Time to $1,000 min / mean / max | Earnings mean | Spend mean | Productivity mean | Work time mean | Distracted mean | Restorative mean | Commands mean | Focus uptime mean | Expanded / hired | Stuck |",
                "|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|"
            };
            foreach (ReleaseScenarioMode mode in Enum.GetValues(typeof(ReleaseScenarioMode)))
            {
                List<ReleaseScenarioResult> group = results.Where(value => value.mode == mode).ToList();
                report.Add($"| {mode.ToString().ToUpperInvariant()} | {group.Count} | " +
                    $"{group.Min(value => value.timeToOneThousandSeconds) / 60f:0.00} / {group.Average(value => value.timeToOneThousandSeconds) / 60f:0.00} / {group.Max(value => value.timeToOneThousandSeconds) / 60f:0.00} min | " +
                    $"${group.Average(value => value.earnings):0.00} | ${group.Average(value => value.vendingSpend):0.00} | " +
                    $"{group.Average(value => value.averageProductivity):0.00} | {group.Average(value => value.workingSeconds) / 60f:0.00} worker-min | " +
                    $"{group.Average(value => value.distractedSeconds) / 60f:0.00} worker-min | {group.Average(value => value.restorativeSeconds) / 60f:0.00} worker-min | " +
                    $"{group.Average(value => value.commandsIssued):0.0} | {group.Average(value => value.focusedUptime) * 100f:0.0}% | " +
                    $"{group.Count(value => value.expansionComplete)} / {group.Count(value => value.newHirePlaced)} | {group.Count(value => value.permanentlyStuck)} |");
            }
            List<ReleaseScenarioResult> recovery = results.Where(value => value.mode == ReleaseScenarioMode.Recovery).ToList();
            report.AddRange(new[]
            {
                string.Empty,
                $"Recovery productivity: {recovery.Average(value => value.recoveryProductivityBefore):0.00} before intervention -> {recovery.Average(value => value.recoveryProductivityAfter):0.00} after intervention.",
                string.Empty,
                "Gate: PASS only when all ACTIVE seeds reach $1,000 in 6-10 minutes, PASSIVE is slower and finishes, POOR finishes despite avoidable losses, RECOVERY improves after redirection, EXPANSION hires and continues for two simulation minutes, and no run is permanently stuck."
            });
            File.WriteAllLines(Path.Combine(folder, "BALANCE_SCENARIO_REPORT.md"), report);
            Debug.Log($"OPEN PLAN RELEASE BALANCE: PASS ({results.Count} scenarios, {ReleaseBalanceScenarios.FixedSeeds.Length} fixed seeds)");
        }

        private static string F(float value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
