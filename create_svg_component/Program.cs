using System;
using System.IO;
using System.Text.RegularExpressions;

namespace create_svg_component
{
    class Program
    {
        static readonly int tabSize = 4;
        static readonly string oneTab = new string(' ', tabSize);

        static string templateStart;
        static string templateEnd;

        static string inputFolder = null;
        static string outputFolder = null;
        static string moveFolder = null;

        static int processedCount = 0;

        static void Main(string[] args)
        {
            ParseArgs(args);

            if (inputFolder == null || !Directory.Exists(inputFolder))
            {
                Console.WriteLine("Error: Input folder not exist.");
                Environment.Exit(1);
            }

            if (outputFolder == null || !Directory.Exists(outputFolder))
            {
                Console.WriteLine("Error: Output folder not exist.");
                Environment.Exit(1);
            }

            string[] files = GetFiles();

            foreach (string fileName in files)
            {
                ParseFile(fileName);
            }

            Console.WriteLine("Done: " + processedCount + " files processed.");
        }

        static void ParseArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--in")
                {
                    inputFolder = args[i + 1];
                    i++;
                }

                if (args[i] == "--out")
                {
                    outputFolder = args[i + 1];
                    i++;
                }

                if (args[i] == "--move")
                {
                    moveFolder = args[i + 1];
                    i++;
                }
            }
        }

        static string[] GetFiles()
        {
            try
            {
                string[] files = Directory.GetFiles(inputFolder, "*.svg", SearchOption.TopDirectoryOnly);
                return files;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return null;
            }
        }

        static void ParseFile(string inputFile)
        {
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(inputFile);
                string fileContents = File.ReadAllText(inputFile);
                SetTemplates(fileName);

                string svg = GetSvg(fileContents);
                svg = RemoveProperties(svg);
                svg = CapitalizeTagFirstLetter(svg);
                svg = AddProps(svg);
                svg = AddTabs(svg);
                svg = CreateComponent(svg);

                string fileNameWithFolder = Path.Combine(outputFolder, fileName + ".tsx");

                if (!File.Exists(fileNameWithFolder))
                {
                    File.WriteAllText(fileNameWithFolder, svg);
                    if (moveFolder != null && Directory.Exists(moveFolder))
                    {
                        string outFile = Path.Combine(moveFolder, Path.GetFileName(inputFile));
                        File.Move(inputFile, outFile);
                    }

                    Console.WriteLine("Component created: " + fileName + ".tsx.");
                    processedCount++;
                }
                else
                {
                    Console.WriteLine("Error: File '" + fileName + ".tsx' already exist.");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        static void SetTemplates(string fileName)
        {
            templateStart = "import * as React from 'react';\nimport Svg, { Path, G, Circle, Rect, Polygon } from 'react-native-svg';\nimport { SvgProps } from '../../types';\n\nconst " + fileName + ": React.FC<SvgProps> = (props) => {\n" + oneTab + "return (\n";
            templateEnd = oneTab + ");\n};\n\nexport default " + fileName + ";\n";
        }

        static string GetSvg(string input)
        {
            string svg = Regex.Match(input, "<svg.*?</svg>", RegexOptions.Singleline | RegexOptions.IgnoreCase).Value;
            svg = Regex.Replace(svg, @"<title>.*?</title>", "");
            svg = Regex.Replace(svg, @"\s\s+", " ");
            svg = Regex.Replace(svg, @"<g>\s*</g>", "", RegexOptions.IgnoreCase);
            svg = Regex.Replace(svg, @">\s+<", "><");

            return svg;
        }

        static string RemoveProperties(string input)
        {
            string[] propertiesToRemove = { "version", "id", "xmlns", "xmlns:xlink", "xml:space", "style" };
            string svg = input;
            
            foreach (string property in propertiesToRemove)
            {
                string pattern = " " + property + "=\".*?\"";
                svg = Regex.Replace(svg, pattern, "");
            }

            return svg;
        }

        static string CapitalizeTagFirstLetter(string input)
        {
            string[] tags = input.Split(">");

            for (int i = 0; i < tags.Length; i++)
            {
                string tag = tags[i];

                if (tag.Trim().Length > 1)
                {
                    if (tag.Substring(1, 1) != "/")
                    {
                        string capitalizedTag = tag.Substring(0, 2).ToUpper() + tag[2..];
                        tags[i] = capitalizedTag;
                    }
                    else
                    {
                        string capitalizedTag = tag.Substring(0, 3).ToUpper() + tag[3..];
                        tags[i] = capitalizedTag;
                    }
                }
            }

            string capitalizedTags = string.Join(">\n", tags);

            return capitalizedTags;
        }

        static string AddProps(string input)
        {
            GroupCollection group = Regex.Match(input, "(<svg.*?)(>.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups;
            string svg = group[1].Value + " {...props}" + group[2].Value;

            return svg;
        }

        static string AddTabs(string input)
        {
            int currentTab = tabSize * 2;
            bool increaseNextTabSize = false;

            string[] lines = input.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.EndsWith("/>"))
                {
                    if (increaseNextTabSize) currentTab += tabSize;
                    increaseNextTabSize = false;
                }
                else if (line.StartsWith("</"))
                {
                    currentTab -= tabSize;
                    increaseNextTabSize = false;
                }
                else if (line.StartsWith("<"))
                {
                    if (increaseNextTabSize) currentTab += tabSize;
                    increaseNextTabSize = true;
                }

                if (line.Trim().Length > 1) lines[i] = new string(' ', currentTab) + line;
            }

            string svg = string.Join('\n', lines);

            return svg;
        }

        static string CreateComponent(string input)
        {
            return templateStart + input + templateEnd;
        }
    }
}
