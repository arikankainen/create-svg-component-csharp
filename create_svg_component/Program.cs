using System;
using System.IO;
using System.Text.RegularExpressions;

namespace create_svg_component
{
    class Program
    {
        static readonly int tabSize = 2;
        static readonly string oneTab = new string(' ', tabSize);

        static string templateStart;
        static string templateEnd;

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string file = args[0];

                if (File.Exists(file))
                {
                    try
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        string fileContents = File.ReadAllText(file);
                        SetTemplates(fileName);

                        string svg = GetSvg(fileContents);
                        svg = RemoveProperties(svg);
                        svg = CapitalizeTagFirstLetter(svg);
                        svg = AddProps(svg);
                        svg = AddTabs(svg);
                        svg = CreateComponent(svg);

                        Console.WriteLine(svg);
                        File.WriteAllText(fileName + "Svg.tsx", svg);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Error: input file not found.");
                }
            }
            else
            {
                Console.WriteLine("Error: input file not specified.");
            }
        }

        static void SetTemplates(string fileName)
        {
            templateStart = "import * as React from 'react';\nimport Svg, { Path, G, Circle, Rect, Polygon } from 'react-native-svg';\nimport { SvgProps } from '../../types';\n\nconst " + fileName + "Svg: React.FC<SvgProps> = (props) => {\n" + oneTab + "return (\n";
            templateEnd = oneTab + ");\n};\n\nexport default " + fileName + "Svg;\n";
        }

        static string GetSvg(string input)
        {
            string svg = Regex.Match(input, "<svg.*?</svg>", RegexOptions.Singleline | RegexOptions.IgnoreCase).Value;
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
                        string capitalizedTag = tag.Substring(0, 2).ToUpper() + tag[2..].ToLower();
                        tags[i] = capitalizedTag;
                    }
                    else
                    {
                        string capitalizedTag = tag.Substring(0, 3).ToUpper() + tag[3..].ToLower();
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
            int currentTab = tabSize;

            string[] lines = input.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith("</")) currentTab -= tabSize;
                else if (line.StartsWith("<")) currentTab += tabSize;

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
