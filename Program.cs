using dnlib.DotNet;
using dnpatch;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace BetterParasyte
{
    public class Program
    {
        static StreamWriter logWriter;
        static string rootAppDirectory;

        const string ParasyteName = "BetterParasyte.dll";


        static void Main(string[] args)
        {
            Log("Discord Location:");
            rootAppDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord");

            Log(rootAppDirectory);

            Log("Do you want install the BetterParasyte? (y/n)");

            if (Console.ReadKey().KeyChar.ToString().ToLower() != "y")
                return;

            Console.WriteLine();

            var updaterPath = Path.Combine(rootAppDirectory, "Update.exe");

            ApplyUpdatePatcher(updaterPath);

            var updaterRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            File.Copy(Assembly.GetExecutingAssembly().Location, Path.Combine(rootAppDirectory, ParasyteName), true);
            File.Copy(Path.Combine(updaterRoot, "dnlib.dll"), Path.Combine(rootAppDirectory, "dnlib.dll"), true);
            File.Copy(Path.Combine(updaterRoot, "dnpatch.dll"), Path.Combine(rootAppDirectory, "dnpatch.dll"), true);

            Log("Done! BetterParasyte Installed.");
        }

        private static void ApplyUpdatePatcher(string Path)
        {
            Patcher thisAssembly = new Patcher(Assembly.GetExecutingAssembly().Location);

            var thisModule = thisAssembly.GetModule();
            var thisClass = thisModule.Types.Where(x => x.Name == "Program").First();

            var injectionMethod = thisClass.Methods.First(m => m.Name == "InjectionLogic");
            Log("Injection Logic Found");

            //var VerificationMethod = thisClass.Methods.First(m => m.Name == "VerificationLogic");
            //Log("Verification Logic Found");

            var ProcessStartMethod = thisClass.Methods.First(m => m.Name == "ProcessStartLogic");
            Log("ProcessStartLogic Logic Found");



            Patcher patcher = new Patcher(new MemoryStream(File.ReadAllBytes(Path)), false);
            var module = patcher.GetModule();


            Log("Target Module Found");

            var ApplyReleasesImpl = module.Types.First(t => t.Name == "UpdateManager")
                                          .NestedTypes.First(t => t.Name == "ApplyReleasesImpl");



            var executeSelfUpdate = ApplyReleasesImpl.Methods.First(m => m.Name == "executeSelfUpdate");
            Log("executeSelfUpdate Logic Found");
            InjectCall(injectionMethod, executeSelfUpdate);

            //var getDirectoryForRelease = ApplyReleasesImpl.Methods.First(m => m.Name == "getDirectoryForRelease");
            //Log("getDirectoryForRelease Logic Found");
            //InjectCall(VerificationMethod, getDirectoryForRelease);

            var program = module.Types.First(t => t.Name == "Program");
            var processStart = program.Methods.First(m => m.Name == "ProcessStart");
            Log("ProcessStart Logic Found");
            InjectCall(ProcessStartMethod, processStart);


            Log("Saving Patched Updater...");

            patcher.Save(Path);
        }

        private static void InjectCall(MethodDef Origin, MethodDef Target)
        {
            var oriCode = Target.Body.Instructions.ToList();
            var injectionCode = Origin.Body.Instructions.Take(Origin.Body.Instructions.Count - 1).ToList();


            if (oriCode.Count > injectionCode.Count)
            {
                var oriPrefix = oriCode.Take(injectionCode.Count).ToList();

                if (oriPrefix.Select(i => i.OpCode).SequenceEqual(injectionCode.Select(i => i.OpCode)))
                {
                    Log("Injection already exists, skipping...");
                    return;
                }
            }


            var finalCode = injectionCode.Concat(oriCode).ToList();

            Log("Updating function body...");

            Target.Body.Instructions.Clear();

            foreach (var instruction in finalCode)
            {
                Target.Body.Instructions.Add(instruction);
            }
        }


        void InjectionLogic(object param)
        {
            Assembly.LoadFile(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), ParasyteName))
                .GetModules()
                .Single()
                .GetType("BetterParasyte.Program")
                .GetMethod("Parasyte", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { param });
        }

        void VerificationLogic(object param)
        {
            Assembly.LoadFile(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), ParasyteName))
                .GetModules()
                .Single()
                .GetType("BetterParasyte.Program")
                .GetMethod("Verification", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { param });
        }


        public void ProcessStartLogic(string exeName, string arguments, bool shouldWait)
        {
            Assembly.LoadFile(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), ParasyteName))
                .GetModules()
                .Single()
                .GetType("BetterParasyte.Program")
                .GetMethod("VerificationOnStart", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { });
        }


        public static void Parasyte(object currentVer)
        {
            dynamic currentVersion = currentVer;

            DirectoryInfo targetDir = getDirectoryForRelease(currentVersion);
            string newSquirrel = Path.Combine(targetDir.FullName, "Squirrel.exe");
            if (!File.Exists(newSquirrel))
            {
                return;
            }
            ApplyUpdatePatcher(newSquirrel);
        }

        public static void Verification(object currentVer)
        {
            dynamic currentVersion = currentVer;

            DirectoryInfo targetDir = getDirectoryForRelease(currentVersion);
            PatchClient(targetDir);
        }

        public static void VerificationOnStart()
        {
            rootAppDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord");

            var last = Directory.GetDirectories(rootAppDirectory)
                .Select(dir => new DirectoryInfo(dir))
                .Where(di => di.Name.StartsWith("app-"))
                .OrderByDescending(di => int.Parse(di.Name.Substring(4).Replace(".", "")))
                .First();

            Log("Lastest Discord Client: " + last.Name);

            PatchClient(last);
        }

        private static bool PatchClient(DirectoryInfo targetDir)
        {
            string ModuleRoot = Path.Combine(targetDir.FullName, "Modules");

            if (!Directory.Exists(ModuleRoot))
            {
                Log("Discord not fully installed yet...");
                return false;
            }

            Log("ModuleRoot: " + ModuleRoot);

            var targetModule = Directory.GetDirectories(ModuleRoot)
                .Select(dir => new DirectoryInfo(dir))
                .Where(di => di.Name.StartsWith("discord_desktop_core"))
                .OrderByDescending(di => di.Name)
                .FirstOrDefault();

            if (targetModule == null)
            {
                Log("Target Discord Desktop Core Not Found...");
                return false;
            }

            Log("Target Module Root: " + targetModule.FullName);

            var moduleContent = targetModule.GetDirectories().Single();

            var moduleIndex = Path.Combine(moduleContent.FullName, "index.js");

            Log("Module Entrypoint: " + moduleIndex);

            if (!File.Exists(moduleIndex))
            {
                Log("Module Index.js Not Found...");
                return false;
            }

            string indexContent = File.ReadAllText(moduleIndex);

            if (indexContent.Contains("betterdiscord"))
            {
                Log("BetterDiscord already installed!");
                return true;
            }

            var betterDiscordEntrypoint = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BetterDiscord", "data", "betterdiscord.asar");
            betterDiscordEntrypoint = betterDiscordEntrypoint.Replace("/", "\\").Replace("\\", "\\\\");

            Log($"Injecting BetterDiscord require... ({betterDiscordEntrypoint})");
            indexContent = $"require(\"{betterDiscordEntrypoint}\");\r\n" + indexContent;
            File.WriteAllText(moduleIndex, indexContent, new UTF8Encoding(true));
            Log("BetterDiscord installed!");
            return true;
        }

        private static void Log(string message)
        {
            Console.WriteLine("[BetterParasyte] " + message);

            try
            {
                if (logWriter == null)
                {
                    var logPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "BetterParasyte.log");
                    logWriter = new StreamWriter(logPath, true);
                }

                logWriter.WriteLine("[BetterParasyte] " + message);
                logWriter.Flush();
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        private static DirectoryInfo getDirectoryForRelease(dynamic releaseVersion)
        {
            return new DirectoryInfo(Path.Combine(rootAppDirectory, "app-" + ((releaseVersion != null) ? releaseVersion.ToString() : null)));
        }
    }
}
