using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Korolow
{
    public class Script
    {
        public string Name { get; set; } = $"{DateTime.Now.ToString("yyyyMMddHHmmss")}";

        #region Properties/Pools

        /*PUBLIC*/

        public List<string> Parameters { get; set; } = new List<string>();

        /*PRIVATE*/

        private List<string> Imports { get; set; } = new List<string>();
        private List<string> Instructions { get; set; } = new List<string>();

        private Process Process => new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "Python.exe",
                Arguments = Path("History", Name),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        private string Path(string directory, string file = null) =>
            $"{Directory.GetCurrentDirectory()}\\{directory}\\{(file?.Insert(file.Length, ".py") ?? string.Empty)}";

        #endregion
        #region Delegates

        public event EventHandler<SuccessEventArgs> Success;
        public event EventHandler<ErrorEventArgs> Error;

        protected virtual void OnSuccess(SuccessEventArgs e) => Success?.Invoke(this, e);
        protected virtual void OnError(ErrorEventArgs e) => Error?.Invoke(this, e);

        #endregion
        #region Methods

        /*PUBLIC*/

        public void Add(params string[] scripts)
        {
            try
            {
                var imports = new List<string>();
                var instructions = new List<string>();

                foreach (string script in scripts)
                {
                    #region Read script

                    string path = Path("Scripts", script);
                    string[] content = File.ReadAllLines(path);

                    #endregion
                    #region Split script into imports and code

                    var lines = Split(content, scripts);

                    imports.AddRange(lines.Imports);
                    instructions.AddRange(lines.Instructions);

                    #endregion
                }

                #region Save imports and code if readed successfully

                Imports.AddRange(imports);
                Instructions.AddRange(instructions);

                #endregion
            }
            catch (Exception e)
            {
                OnError(new ErrorEventArgs(e));
            }
        }

        public void Exec()
        {
            Generate();

            var process = Process;
            try
            {
                #region Load parameters

                foreach (string parameter in Parameters)
                {
                    process.StartInfo.Arguments = $"{process.StartInfo.Arguments} {parameter}";
                }

                #endregion

                using (var outputWaitHandle = new AutoResetEvent(false))
                using (var errorWaitHandle = new AutoResetEvent(false))
                {
                    #region Configure process

                    var output = new StringBuilder();
                    var error = new StringBuilder();

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            output.AppendLine(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            error.AppendLine(e.Data);
                        }
                    };

                    #endregion
                    #region Execute script

                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    #endregion
                    #region Read result

                    if (outputWaitHandle.WaitOne() && errorWaitHandle.WaitOne())
                    {
                        string e = error.ToString();

                        if (!string.IsNullOrWhiteSpace(e))
                        {
                            throw new Exception(message: e);
                        }

                        OnSuccess(new SuccessEventArgs(output.ToString(), (process.ExitTime - process.StartTime).TotalMilliseconds));
                    }

                    #endregion
                }
            }
            catch (Exception e)
            {
                OnError(new ErrorEventArgs(e));
            }
        }

        /*PRIVATE*/

        private void Generate()
        {
            var content = new StringBuilder();

            content.Append(string.Join(Environment.NewLine, Imports));
            content.Append(Environment.NewLine);
            content.Append(string.Join(Environment.NewLine, Instructions));

            var path = new FileInfo(Path("History", Name));

            if (!path.Exists)
            {
                Directory.CreateDirectory(path.Directory.FullName);
            }

            using (StreamWriter file = File.CreateText(path.FullName))
            {
                file.WriteLine(content.ToString().Trim());
            }
        }

        private static (bool IsImport, string Module, string Trimed) Line(string line)
        {
            line = Regex.Replace(line, @"\s+", " ").Trim();

            var regex = new Regex(@"^(from [A-z_]+\w*(.[A-z_]+\w*)* )?import (([A-z_]+\w*(.[A-z_]+\w*)*( as [A-z_]+\w*)?([ ]?,[ ]?[A-z_]+\w*(.[A-z_]+\w*)*( as [A-z_]+\w*)?)*)|\*)$");
            Match match = regex.Match(line);

            return (match.Success, Regex.Split(line, " ")[1], line);
        }

        private (List<string> Imports, List<string> Instructions) Split(string[] lines, string[] scripts)
        {
            var result = (Im: new List<string>(), In: new List<string>());

            result.Im.AddRange(lines.Where(x => Line(x).IsImport && !scripts.Contains(Line(x).Module)).Select(x => Line(x).Trimed));
            result.In.AddRange(lines.Where(x => !Line(x).IsImport));

            return result;
        }

        #endregion
    }   
}