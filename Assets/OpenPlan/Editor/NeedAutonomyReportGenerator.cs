using System;
using System.IO;
using System.Text;
using UnityEditor;

namespace OpenPlan.Editor
{
    public static class NeedAutonomyReportGenerator
    {
        public static void GenerateFromCommandLine()
        {
            NeedAutonomyMatrixReport report = NeedAutonomyDeterministicMatrix.RunAcceptanceMatrix();
            string root = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            string output = Path.Combine(root, "outputs", "TestResults", "02_NeedAutonomy_Simulation_Report.md");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            StringBuilder text = new StringBuilder();
            text.AppendLine("# Checkpoint 02 - Autonomous Need Recovery Simulation");
            text.AppendLine();
            text.AppendLine("- Outcome: **" + (report.Passed ? "PASS" : "FAIL") + "**");
            text.AppendLine("- Matrix: 20 seeds; 3, 10, and 30 workers; 15 scenarios; 10 simulated minutes per row.");
            text.AppendLine("- Extended runs: one 100-minute run for each worker count.");
            text.AppendLine("- Total runs: " + report.Runs);
            text.AppendLine("- Critical needs observed: " + report.CriticalNeeds);
            text.AppendLine("- Average critical threshold-to-response: " + report.AverageCriticalResponseSeconds.ToString("0.00") + " s");
            text.AppendLine("- Average response-to-recovery: " + report.AverageRecoverySeconds.ToString("0.00") + " s");
            text.AppendLine("- Maximum critical duration: " + report.MaximumCriticalSeconds.ToString("0.00") + " s");
            text.AppendLine("- Reservations created/released: " + report.ReservationsCreated + " / " + report.ReservationsReleased);
            text.AppendLine("- Reservation timeouts: " + report.ReservationTimeouts);
            text.AppendLine("- Reroutes: " + report.Reroutes);
            text.AppendLine("- Off-site fallbacks: " + report.OffSiteFallbacks);
            text.AppendLine("- Stuck recoveries / safety corrections: " + report.StuckRecoveries + " / " + report.SafetyCorrections);
            text.AppendLine("- Workers failing to resume: " + report.FailedToResume);
            text.AppendLine("- Desk assignments lost: " + report.LostDeskAssignments);
            text.AppendLine("- Duplicate charges: " + report.DuplicateCharges);
            text.AppendLine("- Invalid need values: " + report.InvalidNeedValues);
            text.AppendLine("- Orphaned reservations / capacity violations: " + report.OrphanedReservations + " / " + report.CapacityViolations);
            text.AppendLine("- Nondeterministic repeats: " + report.NondeterministicRuns);
            text.AppendLine("- Work output / phone output: " + report.WorkOutput.ToString("0.0") + " / " + report.PhoneOutput.ToString("0.0"));
            text.AppendLine("- Active-management advantage: " + report.ActiveAdvantagePercent.ToString("0.0") + "% (10-25% tuning target)");
            text.AppendLine("- Managed-memory observation: " + report.ManagedBytesBefore + " -> " + report.ManagedBytesAfter + " bytes.");
            text.AppendLine();
            text.AppendLine("## Activity selections");
            text.AppendLine();
            for (int i = 0; i < report.ActivitySelections.Length; i++)
                text.AppendLine("- " + ((PlacementActivity)i) + ": " + report.ActivitySelections[i]);
            text.AppendLine();
            text.AppendLine("Performance note: evaluations are staggered and destination scans occur only at deterministic decision intervals. The matrix allocates setup/candidate data and is not a profiler-grade per-frame allocation measurement.");
            File.WriteAllText(output, text.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            if (!report.Passed) throw new InvalidOperationException("Need autonomy deterministic matrix failed. See " + output);
        }
    }
}
