using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CellularAutomataLibrary
{
    public static class CATemplates
    {
        public static void DLA(ushort x, ushort y, ushort z, string path, bool useRings = false)
        {
            DateTime start = DateTime.Now;
            bool alive = true;
            var state0 = ("state", (ushort)0);
            var state1 = ("state", (ushort)1);

            CAIf moveIf = new CAIf("state", CATargetType.All, new CATarget(CAScale.Local, CAEntityType.Agent, new CANeighborhood(CANeighborhoodType.None)), CAEquality.Equal_to, state0);
            CAThenMove moveThen = new CAThenMove(new CANeighborhood(CANeighborhoodType.Edge), new List<double> { 0.25, 0.25, 0.25, 0.25 }, 1);
            CARule moveRule = new CARule(new List<CAIf> { moveIf }, new List<CAThen> { moveThen });


            CA ca = new CA(new ValueTuple<ushort, ushort, ushort>(x, y, z), GridShape.Square);
            var center = new Dictionary<string, dynamic>
            {
                { "state", (ushort)1 }
            };
            ca.AddAgents(new List<ValueTuple<Dictionary<string, dynamic>, ValueTuple<ushort, ushort, ushort>>> { new ValueTuple<Dictionary<string, dynamic>, ValueTuple<ushort, ushort, ushort>>(center, new ValueTuple<ushort, ushort, ushort>(Convert.ToUInt16(x / 2), Convert.ToUInt16(y / 2), Convert.ToUInt16(z / 2))) });
            ca.Settings.StateColorMap = new List<Color> { Color.Red, Color.White };
            ca.Settings.Subprocessing = true;
            ca.Settings.CopyFormat = CACopyFormat.Reference;
            var edge = new Dictionary<string, dynamic>
            {
                { "state", (ushort)0 }
            };

            var minDim = Math.Min(x, y);
            int percent = 100;
            //var minDouble = 1.0;
            //var minShift = Math.Max(1.0 / minDim, 0.01);
            //var curDouble = 1.0;
            if (useRings)
            {
                percent = 1;
                //minDouble = Math.Max(10.0 / minDim, 0.01);
                //curDouble = minDouble;
            }

            CAIf changeIf1 = new CAIf("state", CATargetType.All, new CATarget(CAScale.Local, CAEntityType.Agent, new CANeighborhood(CANeighborhoodType.None)), CAEquality.Equal_to, state0);
            CAIf changeIf2 = new CAIf("state", CATargetType.Any, new CATarget(CAScale.Regional, CAEntityType.Agent, new CANeighborhood(CANeighborhoodType.Edge)), CAEquality.Equal_to, state1);
            CAThenChange changeThen = new CAThenChange(new CATarget(CAScale.Local, CAEntityType.Agent, new CANeighborhood(CANeighborhoodType.None)), "state", CAOperator.Equal, (ushort)1, 1);
            var locationShape = new CALocationShape(CACreationLocationShapeType.Circle, new ValueTuple<ushort, ushort, ushort>(x, y, z), Math.Min(((double)percent / 100.0), 1.0));
            CAThenCreate changeCreate = new CAThenCreate(locationShape, (ushort)0, 1); // this adds ~1 GB for 5001
            CARule changeRule = new CARule(new List<CAIf> { changeIf1, changeIf2 }, new List<CAThen> { changeThen, changeCreate });
            if (useRings)
            {
                (changeRule.Thens[1] as CAThenCreate).Failure += ((object source, CreationEventArgs e) =>
                {
                    if (e.Cause == CreationEventArgs.CreationStatus.Failure_AgentExists)
                    {
                        Console.WriteLine("");
                        if (percent == 100)
                        {
                            Console.WriteLine("Outer ring complete.");
                            alive = false;
                        }
                        else
                        {
                            var locPercents = (((e.Location.Item1 - (x/2.0))/(x/2.0)), ((e.Location.Item2 - (y / 2.0)) / (y / 2.0)), ((e.Location.Item3 - (z / 2.0)) / (z / 2.0)));
                            var maxPer = Math.Max(locPercents.Item1, locPercents.Item2);
                            if (z > 1)
                            {
                                maxPer = Math.Max(maxPer, locPercents.Item3);
                            }
                            percent++;
                            while (maxPer > percent)
                            {
                                percent++;
                            }
                            if (percent > 100)
                            {
                                Console.WriteLine("Outer ring complete.");
                                alive = false;
                            }
                            else
                            {
                                Console.WriteLine("Moving \"add\" ring outward to {0}%", percent);
                                var newPosition = StaticMethods.AddCircle(new ValueTuple<ushort, ushort>(x, y), Math.Min(((double)percent / 100.0), 1.0));
                                if (z != 1)
                                {
                                    newPosition = StaticMethods.AddEllipsoid(new ValueTuple<ushort, ushort, ushort>(x, y, z), Math.Min(((double)percent / 100.0), 1.0));
                                }
                                int pick = (int)Math.Floor(StaticMethods.GetRandomNumber() * newPosition.Count);
                                var newLocationShape = new CALocationShape(CACreationLocationShapeType.Circle, new ValueTuple<ushort, ushort, ushort>(x, y, z), Math.Min(((double)percent / 100.0), 1.0));
                                (changeRule.Thens[1] as CAThenCreate).Location = newLocationShape;
                            }
                        }
                    }
                });
            }
            else
            {
                (changeRule.Thens[1] as CAThenCreate).Failure += ((object source, CreationEventArgs e) =>
                {
                    if (e.Cause == CreationEventArgs.CreationStatus.Failure_AgentExists)
                    {
                        alive = false;
                    }
                });
            }
            (changeRule.Thens[1] as CAThenCreate).Success += ((object source, CreationEventArgs e) =>
            {
                while(ca.Graph.AgentCells.Select(xyz => ca.Graph.GetCell(xyz).Agent.GetStateProperty()).Count() > 1)
                {
                    var location = ca.Graph.AgentCells[0];
                    var cell = ca.Graph.GetCell(location);
                    var stateProperty = cell.Agent.GetStateProperty();
                    if (StaticMethods.CheckEquality(stateProperty, CAEquality.Equal_to, state1.Item2))
                    {
                        ca.Graph.RemoveAgentCell(location);
                    }
                }
            });
            ca.AddRule(changeRule);
            ca.AddRule(moveRule);

            var position = StaticMethods.AddCircle(new ValueTuple<ushort, ushort>(x, y), Math.Min(((double)percent / 100.0), 1.0));
            if (z != 1)
            {
                position = StaticMethods.AddEllipsoid(new ValueTuple<ushort, ushort, ushort>(x, y, z), Math.Min(((double)percent / 100.0), 1.0));
            }

            ca.AddAgents(new List<ValueTuple<Dictionary<string, dynamic>, ValueTuple<ushort, ushort, ushort>>> { new ValueTuple<Dictionary<string, dynamic>, ValueTuple<ushort, ushort, ushort>>(edge, position[1]) });

            Console.WriteLine("Press Enter to begin.");
            Console.ReadKey();
            //Console.WriteLine("When you see the task complete line below, press any key to continue.");
            DateTime now = DateTime.Now;
            Console.WriteLine("Dimensions: {0}, {1}, {2}", x, y, z);
            Task output = Task.Factory.StartNew(() =>
            {
                while (alive && !(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
                {
                    ca.Run();
                    Console.Write("\rIteration {0} {1}   ", ca.Iteration, ca.Graph.AgentCells.Last().ToString());

                }
                Console.WriteLine("");
            });
            output.Wait();
            Console.WriteLine("Task complete after: " + (DateTime.Now - now).TotalSeconds + " seconds");
            var image = StaticMethods.MakeImage(ca);
            string filename = path + System.IO.Path.DirectorySeparatorChar + start.ToString("o").Replace(':', '.') + " Iteration " + ca.Iteration + ".bmp";
            image.Save(filename, System.Drawing.Imaging.ImageFormat.Bmp);
            Console.WriteLine("Saved to {0}.", filename);
            Console.WriteLine("Complete. Press any key to exit.");
            Console.ReadKey();
        }

        public static void ChemicalEquilibrium(ValueTuple<ushort, ushort, ushort> dimensions, List<List<double>> probabilities, string savePath, ValueTuple<ulong, double, double> convergeanceConditions, CASettings settings = null)
        {
            List<(string, dynamic)> stateProperties = new List<(string, dynamic)>();
            for (int i = 0; i < probabilities.Count; i++)
            {
                if (probabilities[i].Count != (probabilities.Count - 1))
                {
                    throw new Exception("Each state must include a probability of conversion to every other state (NOT its own)");
                }
                stateProperties.Add(("state", (ushort)i));
            }
            var noneNeighborhood = new CANeighborhood(CANeighborhoodType.None);
            var localTarget = new CATarget(CAScale.Local, CAEntityType.Agent, noneNeighborhood);
            CA ca = new CA(dimensions, GridShape.Square);
            if(settings != null)
            {
                ca.Settings = settings;
            }
            else
            {
                ca.Settings = new CASettings { CopyFormat = CACopyFormat.Reference, StateColorMap = StaticMethods.GetColors(probabilities.Count), /*StoreChangeCounts = true,*/ StoreCounts = true, Subprocessing = false, StoreTransitions = true };
            }
            ca.Settings.States = (ushort)probabilities.Count;
            for (int i = 0; i < probabilities.Count; i++)
            {
                ca.AddAgents((1.0 / probabilities.Count), (ushort)i, true);
                int val = 0;
                CAIf stateif = new CAIf("state", CATargetType.All, localTarget, CAEquality.Equal_to, stateProperties[i]);
                for (int j = 0; j < probabilities.Count; j++)
                {
                    if(i == j)
                    {
                        continue;
                    }
                    CAThenChange changeThen = new CAThenChange(localTarget, "state", CAOperator.Equal, (ushort)j, probabilities[i][val]);
                    CARule changeRule = new CARule(new List<CAIf> { stateif }, new List<CAThen> { changeThen });
                    ca.AddRule(changeRule);
                    val++;
                }
            }
            CAExitCondition exit = new CAExitConditionConvergeance(convergeanceConditions.Item1, convergeanceConditions.Item2, convergeanceConditions.Item3);
            ca.CreateExitCondition(exit);
            DateTime start = DateTime.Now;
            //bool alive = true;
            Console.WriteLine("Every iteration, the CA will check that the population counts have changed by less than " + (convergeanceConditions.Item2 > 1? ((int)convergeanceConditions.Item2).ToString() + " cells":(convergeanceConditions.Item2 * 100).ToString() + "%") + " (for a single state).");
            Console.WriteLine("It will then check for >" + convergeanceConditions.Item1 + " iterations, then " + (convergeanceConditions.Item3 > 1 ? ((int)convergeanceConditions.Item3).ToString() + " iterations" : (convergeanceConditions.Item3 * 100).ToString() + "%") + " of the total iterations, where the previous predicate is true (consecutively).");
            Console.WriteLine("It will automatically end at that time, but you can exit early by pressing Escape. Your data up to that point will be saved.");
            Console.WriteLine("Press Enter to begin.");
            Console.ReadKey();
            DateTime now = DateTime.Now;
            Task output = Task.Factory.StartNew(() =>
            {
                while (!ca.Exit && !(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
                {
                    ca.Run();
                    Console.Write("\rIteration {0}, Counts: {1}", ca.Iteration, String.Join(", ", ca.CurrentCounts) + "                ");
                }
                Console.WriteLine("");
            });
            output.Wait();
            Console.WriteLine("Task complete after: " + (DateTime.Now - now).TotalSeconds + " seconds");
            var probString = probabilities.Select(x => x.Select(y => y.ToString()).ToList()).ToList();
            for (int i = 0; i < probString.Count; i++)
            {
                probString[i].Insert(0, i.ToString());
            }
            // save data
            List<List<string>> header = new List<List<string>>()
            {
                new List<string>{now.ToString("o") },
                new List<string>{"Chemical Equilibrium"},
                new List<string>{"Probabilities"}
            };
            header.AddRange(probString);
            ca.Save(header, ca.ListSaveProperties(), savePath + System.IO.Path.DirectorySeparatorChar + start.ToString("o").Replace(':', '.') + " Iteration " + ca.Iteration + " Data.csv");
            Console.WriteLine("Data saved to: " + savePath + System.IO.Path.DirectorySeparatorChar + start.ToString("o").Replace(':', '.') + " Iteration " + ca.Iteration + " Data.csv");
            Console.WriteLine();
            Console.WriteLine("Enter Y to save image, and anything else to skip.");
            bool saveImage = false;
            var key = Console.ReadLine();
            if (key.ToLower().Equals("y"))
            {
                saveImage = true;
            }
            else
            {
                Console.WriteLine("Are you sure? Enter Y to save image, and anything else to skip.");
                key = Console.ReadLine();
                if (key.ToLower().Equals("y"))
                {
                    saveImage = true;
                }
            }
            if(saveImage)
            {
                var image = StaticMethods.MakeImage(ca);
                string filename = savePath + System.IO.Path.DirectorySeparatorChar + start.ToString("o").Replace(':', '.') + " Iteration " + ca.Iteration + " Image.bmp";
                image.Save(filename, System.Drawing.Imaging.ImageFormat.Bmp);
                Console.WriteLine("Saved to {0}.", filename);
            }
            Console.WriteLine("Complete. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
