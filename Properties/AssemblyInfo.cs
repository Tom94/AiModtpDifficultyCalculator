using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using osu.GameModes.Edit.AiMod;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("AiModtpDifficultyCalculator")]
[assembly: AssemblyDescription("The osu!tp difficulty calculator for AiMod")]
[assembly: AssemblyProduct("AiModtpDifficultyCalculator")]
[assembly: AssemblyCopyright("Copyright © Tom94 2013")]

//THIS LINE IS IMPORTANT! You need to include your assembly name (which should match the filename of your dll) and the Ruleset you wish to load.
[assembly: AiModRulesetAttribute("AiModtpDifficultyCalculator", "AiModtpDifficultyCalculator.AiModtpDifficulty")]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
