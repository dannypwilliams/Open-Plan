using System;

namespace OpenPlan
{
    /// <summary>Owns deterministic stage selection across menu and scene loads.</summary>
    public static class OfficeStageSelection
    {
        public const string StageArgument = "-openplan-stage";
        private static OfficeStage? nextStage;

        public static OfficeStage Resolve(string[] arguments, OfficeStage fallback = OfficeStage.StarterOffice)
        {
            arguments ??= Array.Empty<string>();
            for (int i = 0; i < arguments.Length; i++)
            {
                string argument = arguments[i] ?? string.Empty;
                if (argument.StartsWith(StageArgument + "=", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParse(argument.Substring(StageArgument.Length + 1), out OfficeStage inlineStage))
                        return inlineStage;
                }
                else if (string.Equals(argument, StageArgument, StringComparison.OrdinalIgnoreCase) && i + 1 < arguments.Length)
                {
                    if (TryParse(arguments[i + 1], out OfficeStage followingStage))
                        return followingStage;
                }
            }

            // Legacy release automation targets the preserved large-office evidence
            // unless a stage is explicitly supplied.
            if (HasArgument(arguments, "-openplan-capture") ||
                HasArgument(arguments, "-openplan-video") ||
                HasArgument(arguments, "-openplan-performance") ||
                HasArgument(arguments, "-openplan-verify-package"))
                return OfficeStage.EstablishedOffice;

            return fallback;
        }

        public static void SelectForNextLoad(OfficeStage stage) => nextStage = stage;

        public static OfficeStage ConsumeForOffice()
        {
            if (nextStage.HasValue)
            {
                OfficeStage selected = nextStage.Value;
                nextStage = null;
                return selected;
            }
            return Resolve(Environment.GetCommandLineArgs());
        }

        public static void ClearPendingSelection() => nextStage = null;

        private static bool HasArgument(string[] arguments, string wanted)
            => Array.Exists(arguments, argument => string.Equals(argument, wanted, StringComparison.OrdinalIgnoreCase));

        private static bool TryParse(string value, out OfficeStage stage)
        {
            if (Enum.TryParse(value, true, out stage)) return true;
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "starter": stage = OfficeStage.StarterOffice; return true;
                case "expanded": stage = OfficeStage.StarterOfficeExpanded; return true;
                case "established": stage = OfficeStage.EstablishedOffice; return true;
                default: stage = OfficeStage.StarterOffice; return false;
            }
        }
    }
}
