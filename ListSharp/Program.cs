﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ListSharp
{
    class Program
    {

        static void Main(string[] args)
        {
            Boolean debugmode = true;
            Console.ForegroundColor = ConsoleColor.DarkCyan;

            Console.BackgroundColor = ConsoleColor.White;

            Console.Title = "ListSharp Console";
            Console.Clear();
            Regex _regex;
            Match match;
            string currentdir = Environment.CurrentDirectory;
            string allcode = System.IO.File.ReadAllText(currentdir + "\\ListSharp.ls");
            allcode = allcode.Replace("<here>", currentdir);
            if (debugmode == true)
            {
                Console.WriteLine("Original Code \n-----------------------");
                Console.WriteLine(allcode);
                Console.WriteLine("-----------------------");
                Console.WriteLine(Environment.NewLine);
            }
            List<string> maincode = allcode.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList();
            maincode.RemoveAll(string.IsNullOrWhiteSpace);
            List<string> alllists = new List<string>();
            string code = "public class MainClass" + Environment.NewLine + "{ " + Environment.NewLine + "public string Execute()" + Environment.NewLine + "{" + Environment.NewLine + "string temp_contents = \"\";" + Environment.NewLine + "string[][] tempstrarr;" + Environment.NewLine + "string output = \"\";" + Environment.NewLine;
            //variables initialization starts here:
            foreach (string singleline in maincode) {



                //rows variable
                if (singleline.Substring(0, 4) == "ROWS")
                {
                    _regex = new Regex(@"ROWS([^>]*)=");
                    match = _regex.Match(singleline);
                    alllists.Add("string[] " + match.Groups[1].Value.Trim() + ";" + Environment.NewLine);
                }

                //strg variable
                if (singleline.Substring(0, 4) == "STRG")
                {
                    _regex = new Regex(@"STRG([^>]*)=");
                    match = _regex.Match(singleline);
                    alllists.Add("string " + match.Groups[1].Value.Trim() + ";" + Environment.NewLine);
                }

            }

            /*
             * since it creates the appropiate code for each variable even if it occurs twice
             * i am making sure there are no duplicate variable initalizations which would break the code
             */
            alllists = alllists.Distinct().ToList<string>();
            foreach (string temp_string in alllists)
            {
                code += temp_string;
            }

            int line = 0;
            foreach (string singleline in maincode)
            {


                line++; //to know what line number we are currently processing


                string[] splitline = singleline.Split(new char[] { '=' }, 2); //splitting the line of "variable = evaluated string later to be parsed
                splitline[1] = splitline[1].Substring(1);

                if (splitline[0].Substring(0, 2) == "//") //to see if the code is commented out so it does net get into the final code (replaced with //skipped for debugging porpuses
                {
                    code += "//skipped";
                    code += Environment.NewLine;
                    continue;
                }


                if (splitline[0].Length < 4 || splitline[1].Length < 3) //checking that there is not too short/impossible line to break the interperter
                {
                    Console.WriteLine("ListSharp exception: Line: " + line + " invalid");
                    while(true)
                    {
                        Thread.Sleep(1000);
                    }
                    
                }


                string varname = splitline[0].Substring(4).Trim(); //the first 4 characters will always be the variable type ex: strg,rows,list

                //strg variable type
                if (splitline[0].Substring(0, 4) == "STRG")
                {



                    if (splitline[1].Substring(0, 4) == "READ") //read text file into code command is called "read"
                    {
                        _regex = new Regex(@"READ\[([^>]*)\]"); //everything between the square brackets "[path]"
                        match = _regex.Match(splitline[1]);
                        string path = @match.Groups[1].Value.Trim();

                        if (!File.Exists(path)) //checking that the file is readable
                        {
                            Console.WriteLine("ListSharp exception: File does not exist, aborting operation\nadditional information: File:\"" + path + "\"");

                            while (true)
                            {
                                Thread.Sleep(1000); //sleep forever
                            }
                        }
                        code += "temp_contents = System.IO.File.ReadAllText(@\"" + path + "\");"; //create the reading file code in interperted form that is read into a tempoary variable
                        code += Environment.NewLine;

                    }

                    code += varname + " = temp_contents;"; //set variable to tempoary variable
                    
                }

                //rows variable type
                if (splitline[0].Substring(0, 4) == "ROWS")
                {
                    if (splitline[1].Substring(0, 8) == "ROWSPLIT") //rowsplit command
                    {
                        _regex = new Regex(@"ROWSPLIT([^>]*)BY"); //this finds what variable is to be split
                        match = _regex.Match(splitline[1]);
                        string invar = match.Groups[1].Value.Trim();

                        _regex = new Regex(@"\[(.*)\]"); //this finds by what string to split the variable
                        match = _regex.Match(splitline[1]);
                        string bywhat = match.Groups[1].Value.Trim();

                        bywhat = bywhat.Replace("<newline>", "System.Environment.NewLine"); //so you can split by newline by saying the string is <newline>

                        code += varname + " = " + invar + ".Split(new string[] { " + bywhat + " }, System.StringSplitOptions.None);"; //interperted code
                    }

                    if (splitline[1].Substring(0, 8) == "GETLINES") //rowsplit command
                    {
                        _regex = new Regex(@"GETLINES([^>]*)\["); //this finds what variable is to be split
                        match = _regex.Match(splitline[1]);
                        string invar = match.Groups[1].Value.Trim();

                        _regex = new Regex(@"\[(.*)\]"); //this finds what lines of the array to get
                        match = _regex.Match(splitline[1]);
                        string bywhat = match.Groups[1].Value.Trim();

                        code += varname + " = getlines(" + invar + ",\"" + bywhat + "\");"; //interperted code
                    }

                    if (splitline[1].Substring(0, 7) == "EXTRACT") //extract command
                    {
                        _regex = new Regex(@"EXTRACT([^>]*)FROM"); 
                        match = _regex.Match(splitline[1]);
                        string collumvar = match.Groups[1].Value.Trim();

                        _regex = new Regex(@"\[(.*)\]"); //this finds what collum is to be extracted
                        match = _regex.Match(collumvar);
                        int collumnum = Convert.ToInt32(match.Groups[1].Value.Trim());

                        _regex = new Regex(@"FROM([^>]*)SPLIT"); //this finds what variable is to be extracted from
                        match = _regex.Match(splitline[1]);
                        string whatvar = match.Groups[1].Value.Trim();

                        _regex = new Regex(@"BY \[(.*)\]"); //this finds by what string to extract the variable
                        match = _regex.Match(splitline[1]);
                        string bywhat = match.Groups[1].Value;


                        code += varname + " = Extract(" + whatvar + "," + bywhat + "," + collumnum + ");"; //interperted code
                    }



                    if (splitline[1].Substring(0, 7) == "COMBINE") //extract command
                    {

                        _regex = new Regex(@"COMBINE\[([^>]*)\]"); //everything between the square brackets "[array,array,array]"
                        match = _regex.Match(splitline[1]);
                        string combinearrays = @match.Groups[1].Value.Trim();

                        _regex = new Regex(@"WITH \[(.*)\]"); //this finds by what string to extract the variable
                        match = _regex.Match(splitline[1]);
                        string withwhat = match.Groups[1].Value;

                        code += "tempstrarr = new string[][] {" + combinearrays + "};";
                        code += Environment.NewLine;
                        code += varname + " = Combine(tempstrarr," + withwhat + ");"; //interperted code
                    }



                }




                if (splitline[0].Substring(0, 4) == "SHOW") //show a variable to debug your program
                {
                        _regex = new Regex(@"([^>]*);");
                        match = _regex.Match(splitline[1]);
                        code += "output = makeOutput(" + match.Groups[1].Value.Trim() + " , output);"; //call makeOutput() on any type of variable the users wants to display
                        code += Environment.NewLine;
                }

                if (splitline[0].Substring(0, 4) == "OUTP") //output command
                {
                    _regex = new Regex(@"([^>]*)HERE"); //everything between the square brackets "[path]"
                    match = _regex.Match(splitline[1]);
                    string thevar = @match.Groups[1].Value.Trim();


                    _regex = new Regex(@"HERE\[([^>]*)\]"); //everything between the square brackets "[path]"
                    match = _regex.Match(splitline[1]);
                    string path = @match.Groups[1].Value.Trim();

                    code += "Write(@\"" + path + "\", " + thevar + ");"; //output the rows to file
                    code += Environment.NewLine;
                }

                code += Environment.NewLine;
            }

            code += "return output;";

            code += Environment.NewLine + "}" + Environment.NewLine;

            
            code += "public string getString(object thevar)";
            code += Environment.NewLine;
            code += "{"; 
            code +=  Environment.NewLine;
            code +=  "if (thevar.GetType() == typeof(string))"; 
            code +=  Environment.NewLine;
            code += "return \"[0]\" + (string)thevar + \"[/0]\";"; 
            code +=  Environment.NewLine; 
            code +=  "if (thevar.GetType() == typeof(string[]))"; 
            code +=  Environment.NewLine;
            code += "return arr2str((string[])thevar);"; 
            code +=  Environment.NewLine;
            code +=  "return \"Show Error\";";
            code +=  Environment.NewLine ;
            code +=  "}";

            code += Environment.NewLine;
            code += Environment.NewLine;
            code += "public string makeOutput(object thein,string output)";
            code += Environment.NewLine;
            code += "{";
            code += Environment.NewLine;
            code += "output += System.Environment.NewLine + \"---------------output--------------\" + System.Environment.NewLine + getString(thein).Replace(System.Environment.NewLine,\"<newline>\" + System.Environment.NewLine) + System.Environment.NewLine + \"-----------------------------------\" + System.Environment.NewLine;";
            code += Environment.NewLine;
            code += "return output;";
            code += Environment.NewLine;
            code += "}";

            code += Environment.NewLine;
            code += Environment.NewLine;
            code += "public string arr2str(string[] arr)";
            code += Environment.NewLine;
            code += "{";
            code += Environment.NewLine;
            code += "string r = \"\";";
            code += Environment.NewLine;
            code += "for (int i = 0; i < arr.Length; i++)";
            code += Environment.NewLine;
            code += "r += \"[\" + i + \"]\" + arr[i] + \"[/\" + i + \"]\";";
            code += Environment.NewLine;
            code += "return r;";
            code += Environment.NewLine;
            code += "}";
            code += Environment.NewLine;
            code += Environment.NewLine;

            code += "public string[] Extract(string[] arr,string bywhat,int collumnum)";
            code += Environment.NewLine;
            code += "{";
            code += Environment.NewLine;
            code += "collumnum--;";
            code += Environment.NewLine;
            code += "string[] restr = new string[arr.Length];";
            code += Environment.NewLine;
            code += "for (int i = 0; i < arr.Length; i++)";
            code += Environment.NewLine;
            code += "{";
            code += Environment.NewLine;
            code += "restr[i] = arr[i].Split(new string[] {bywhat}, System.StringSplitOptions.None)[collumnum];";
            code += Environment.NewLine;
            code += "}";
            code += Environment.NewLine;
            code += "return restr;";
            code += Environment.NewLine;
            code += "}";

            code += Environment.NewLine;
            code += "public string[] Combine(string[][] srar,string bywhat)";
            code += Environment.NewLine;
            code += "{";
            code += Environment.NewLine;
            code += "int max = 0;";
            code += Environment.NewLine;
            code += "for (int i = 0; i<srar.Length; i++)";
            code += Environment.NewLine;
            code += "{";
            code += Environment.NewLine;
            code += "if (srar[i].Length > max)";
            code += Environment.NewLine;
            code += "max = srar[i].Length;";
            code += Environment.NewLine;
            code += "}";
            code += Environment.NewLine;
            code += "string[] tempo = new string[max];";
            code += Environment.NewLine;
            code += "string sinline = \"\";";
            code += Environment.NewLine;
            code += "for (int i = 0; i < max; i++)";
            code += Environment.NewLine;
            code += "{";
            code += Environment.NewLine;
            code += "for (int j = 0; j < srar.Length; j++)";
            code += Environment.NewLine;
            code += "{";
            code += Environment.NewLine;
            code += "if (i < srar[j].Length)";
            code += Environment.NewLine;
            code += "sinline += srar[j][i];";
            code += Environment.NewLine;
            code += "if (j != (srar.Length - 1))";
            code += Environment.NewLine;
            code += "sinline += bywhat;";
            code += Environment.NewLine;
            code += "}";
            code += Environment.NewLine;
            code += "tempo[i] = sinline;";
            code += Environment.NewLine;
            code += "sinline = \"\";";
            code += Environment.NewLine;
            code += "}";
            code += Environment.NewLine;
            code += "return tempo;";
            code += Environment.NewLine;
            code += "}";
            code += Environment.NewLine;
            code += Environment.NewLine;

            code += "public void Write(string path, object thevar)";
            code += Environment.NewLine;
            code += "{";
            code += Environment.NewLine;
            code += "if (thevar.GetType() == typeof(string))";
            code += Environment.NewLine;
            code += "{";
            code += Environment.NewLine;
            code += "System.IO.File.WriteAllText(path,(string)thevar);";
            code += Environment.NewLine;
            code += "}";
            code += Environment.NewLine;
            code += "if (thevar.GetType() == typeof(string[]))";
            code += Environment.NewLine;
            code += "{";
            code += Environment.NewLine;
            code += "System.IO.File.WriteAllLines(path,(string[])thevar);";
            code += Environment.NewLine;
            code += "}";
            code += Environment.NewLine;
            code += "}";
            code += Environment.NewLine;

            code += Environment.NewLine;
            code += Environment.NewLine;
            code += "public string[] getlines(string[] ins,string lines)";
            code += Environment.NewLine;
            code += "{";
            code += Environment.NewLine;
            code += "string[] findrange = lines.Split(',');";
            code += Environment.NewLine;
            code += "System.Collections.Generic.List<int> range = new System.Collections.Generic.List<int>();";
            code += Environment.NewLine;
            code += "for (int i = 0; i < findrange.Length; i++)";
            code += Environment.NewLine;
            code += "{";
            code += Environment.NewLine;
            code += "if (findrange[i].Contains(\"-\"))";
            code += Environment.NewLine;
            code += "{";
            code += Environment.NewLine;
            code += "int start = System.Convert.ToInt32(findrange[i].Split('-')[0]);";
            code += Environment.NewLine;
            code += "int end = System.Convert.ToInt32(findrange[i].Split('-')[1]);";
            code += Environment.NewLine;
            code += "for (int j = start; j<=end; j++)";
            code += Environment.NewLine;
            code += "{";
            code += Environment.NewLine;
            code += "range.Add(j);";
            code += Environment.NewLine;
            code += "}";
            code += Environment.NewLine;
            code += "}";
            code += Environment.NewLine;
            code += "else";
            code += Environment.NewLine;
            code += "{";
            code += Environment.NewLine;
            code += "range.Add(System.Convert.ToInt32(findrange[i]));";
            code += Environment.NewLine;
            code += "}";
            code += Environment.NewLine;
            code += "}";
            code += Environment.NewLine;
            code += "string[] tempret = new string[range.Count];";
            code += Environment.NewLine;
            code += "for (int i = 0; i < range.Count; i++)";
            code += Environment.NewLine;
            code += "tempret[i] = range[i].ToString();";
            code += Environment.NewLine;
            code += "return tempret;";
            code += Environment.NewLine;
            code += "}";
            code += Environment.NewLine;
            code += "}";
            code += Environment.NewLine;

            code += Environment.NewLine;
            code += Environment.NewLine;
            
            if (debugmode == true)
            {
                Console.WriteLine(Environment.NewLine + Environment.NewLine + Environment.NewLine);
                Console.WriteLine("Interperted Code \n-----------------------");
            }
            int index = 1;
            char[] delimiterChars = { '\n' };
            string[] allLines = code.Split(delimiterChars);
            

            foreach (string currentLine in allLines)
            {
                if (debugmode == true)
                System.Console.WriteLine("Line: " + index + "    " +currentLine);
                index++;
            }


            if (debugmode == true)
            {
                Console.WriteLine("-----------------------");
                Console.WriteLine(Environment.NewLine + Environment.NewLine + Environment.NewLine + Environment.NewLine);
            }
            Console.WriteLine("Initializing ListSharp");

            //compiling starts here

            using (Microsoft.CSharp.CSharpCodeProvider CodeProv =
            new Microsoft.CSharp.CSharpCodeProvider())
            {
                CompilerResults results = CodeProv.CompileAssemblyFromSource(
                     new System.CodeDom.Compiler.CompilerParameters()
                     {
                         GenerateInMemory = true
                     },
                     code);
                foreach (CompilerError er in results.Errors)
                {
                    Console.WriteLine(er.ToString());
                }
                if (results.Errors.HasErrors)
                {
                    Console.WriteLine(Environment.NewLine + "Got compiling errors,aborting ListSharp");
                    while (true)
                    {
                        Thread.Sleep(1000);
                    }
                }


                 
                 var type = results.CompiledAssembly.GetType("MainClass");

                 var obj = Activator.CreateInstance(type);

                 var output = type.GetMethod("Execute").Invoke(obj, new object[] { });
                 
                 Console.WriteLine(output);
            }

            while (true)
            {
                Thread.Sleep(1000); //sleep forever
            }
        }
    }
}


