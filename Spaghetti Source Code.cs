using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.Threading.Tasks;

//(Modified) MIT License

//Copyright(c) 2018 BananaCzar aka ArchOfRebellion

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//If we meet someday and you think this project was worth it, you can buy me a
//beer.

//Obviously, Stellaris is the property of Paradox Plaza. May their copyrights
//reign supreme over this license and all of their work.

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

    //Since last update:
    //Added titans as possibility
    //Added hull upgrades, allowing better comparison between ship classes
    //Added intelligence weights, allowing better optimization against fleets (optional)
    //Fixed a bug where reactors were not being written to output file
    //Fixed a bug where other ship's auxiliary defenses would be added to new ships
    //Utilities are now written at the end of each ship, instead of at the end of each section
    //Added guardian & crisis support for intelligence - note that changes were made to weapons.csv , utilities.csv , and defenses.csv
    
    //Still to add:
    //Difficulty scaler
    //Improve missile defense


namespace Stellaris_Ship_Optimizer
{
    class Program
    {
        private static Dictionary<string, int> Tech;
        static void Main(string[] args)
        {

            //Get the input folder location
            //FolderBrowserDialog fbd = new FolderBrowserDialog();
            string FolderPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Console.WriteLine(FolderPath);
            //if(fbd.ShowDialog() == DialogResult.OK)
            //{
            //    FolderPath = fbd.SelectedPath;
            //}
            //Dictionary Counter - This makes sure everything was loaded correctly.. It should eventually be equal to 6
            int DictCounter = 0;
            //Load everything
            Dictionary<string, weapon> WeaponDictionary = ReadWeaponDictionary(FolderPath + "\\weapons.csv");            
            if (WeaponDictionary.Count > 0)
            {
                Console.WriteLine(WeaponDictionary.Count + " Weapons loaded.");
                DictCounter++;
            }
            Dictionary<string, defense> DefenseDictionary = ReadDefenseDictionary(FolderPath + "\\defenses.csv");
            if (DefenseDictionary.Count > 0)
            {
                Console.WriteLine(DefenseDictionary.Count + " Defenses loaded.");
                DictCounter++;
            }
            Dictionary<string, aux> AuxiliaryDictionary = ReadAuxDictionary(FolderPath + "\\auxiliaries.csv");
            if (AuxiliaryDictionary.Count > 0)
            {
                Console.WriteLine(AuxiliaryDictionary.Count + " Auxiliaries loaded.");
                DictCounter++;
            }
            Dictionary<string, utility> UtilityDictionary = ReadUtilityDictionary(FolderPath + "\\utilities.csv");
            if (UtilityDictionary.Count > 0)
            {
                Console.WriteLine(UtilityDictionary.Count + " Utilities loaded.");
                DictCounter++;
            }
            List<BadGuy> Badguys = LoadIntelligence(FolderPath + "\\intelligence.csv", WeaponDictionary, DefenseDictionary, AuxiliaryDictionary, UtilityDictionary);
            if (Badguys.Count > 0)
            {
                Console.WriteLine("Intelligence loaded. I've got data on " + Badguys.Count + " enemies.");
                DictCounter++;
            }
            Tech = LoadTech(FolderPath + "\\tech.csv");
            if (Tech.Count > 0)
            {
                Console.WriteLine("Tech loaded. I've got data on " + Tech.Count + " technologies.");
                DictCounter++;
            }
            if (DictCounter == 6)
            {
                List<weapon> AvailableWeapons = new List<weapon>();
                List<defense> AvailableDefenses = new List<defense>();
                List<aux> AvailableAuxiliary = new List<aux>();
                List<utility> AvailableUtilities = new List<utility>();
                //Load weapons
                foreach (var Weapon in WeaponDictionary)
                {
                    if (Tech.ContainsKey(Weapon.Value.tech_tree) && Tech[Weapon.Value.tech_tree] == Weapon.Value.tier)
                    {
                        AvailableWeapons.Add(Weapon.Value);
                        //Console.WriteLine(Weapon.Value.name + " is available.");
                    }
                }
                //Load defenses
                foreach (var Defense in DefenseDictionary)
                {
                    if (Tech.ContainsKey(Defense.Value.tech_tree) && Tech[Defense.Value.tech_tree] == Defense.Value.tier)
                    {
                        AvailableDefenses.Add(Defense.Value);
                        //Console.WriteLine(Defense.Value.name + " is available.");
                    }
                }
                //Load auxiliaries
                foreach (var Aux in AuxiliaryDictionary)
                {
                    if (Tech.ContainsKey(Aux.Value.tech_tree) && Tech[Aux.Value.tech_tree] == Aux.Value.tier)
                    {
                        AvailableAuxiliary.Add(Aux.Value);
                        //Console.WriteLine(Aux.Value.name + " is available.");
                    }
                }
                //Load utilities
                foreach (var Utility in UtilityDictionary)
                {
                    //Note that this only checks if it is equal, thereby pulling only the best equipment
                    if (Tech.ContainsKey(Utility.Value.tech_tree) && Tech[Utility.Value.tech_tree] == Utility.Value.tier)
                    {
                        AvailableUtilities.Add(Utility.Value);
                        //Console.WriteLine(Utility.Value.name + " is available.");
                    }
                }
                //Optimize best versions
                List<ship> BestShips = new List<ship>();
                if (Tech["Corvette"] > 0)
                {
                    corvette BestCorvette = ImpOptimizeCorvette(AvailableWeapons, AvailableDefenses, AvailableAuxiliary, AvailableUtilities, Badguys);
                    BestShips.Add(BestCorvette);
                    //BestCorvette.ReadOut();
                }
                if (Tech["Destroyer"] > 0)
                {
                    
                    destroyer BestDestroyer = ImpOptimizeDestroyer(AvailableWeapons, AvailableDefenses, AvailableAuxiliary, AvailableUtilities, Badguys);
                    //BestDestroyer.ReadOut();
                    BestShips.Add(BestDestroyer);
                }
                if (Tech["Cruiser"] > 0)
                {
                    cruiser BestCruiser = ImpOptimizeCruiser(AvailableWeapons, AvailableDefenses, AvailableAuxiliary, AvailableUtilities, Badguys);
                    //BestCruiser.ReadOut();
                    BestShips.Add(BestCruiser);
                }
                if (Tech["Battleship"] > 0)
                {
                    battleship BestBattleship = ImpOptimizeBattleship(AvailableWeapons, AvailableDefenses, AvailableAuxiliary, AvailableUtilities, Badguys);
                    //BestBattleship.Readout();
                    BestShips.Add(BestBattleship);
                }
                if (Tech["Platform"] > 0)
                {
                    platform BestPlatform = ImpOptimizePlatform(AvailableWeapons, AvailableDefenses, AvailableAuxiliary, AvailableUtilities, Badguys);
                    //BestPlatform.ReadOut();
                    BestShips.Add(BestPlatform);
                }
                if(Tech["Titan"] > 0)
                {
                    titan BestTitan = ImpOptimizeTitan(AvailableWeapons, AvailableDefenses, AvailableAuxiliary, AvailableUtilities, Badguys);
                    //BestTitan.Readou();
                    BestShips.Add(BestTitan);
                }
                WriteResults(BestShips, FolderPath + "\\" + Badguys[0].name + "results-all.csv");
                Console.WriteLine();
                Console.WriteLine("Analysis Complete!!");
                Console.WriteLine("Results written to: " + FolderPath + "\\" + Badguys[0].name + "results-all.csv");
                //Console.WriteLine("Press Any Key to Exit.");
            }
            else if(DictCounter == 0)
            {
                Console.WriteLine("This does not appear to be a valid folder. Are you sure you picked the right path?");
                Console.WriteLine("The path you gave me was: " + FolderPath);
            }
            else
            {
                Console.WriteLine((6 - DictCounter) + " files seem to be missing in this folder.");
            }
            Console.ReadKey();
        }

        //Reads in list of weapons
        public static List<weapon> ReadWeapons(string FilePath)
        {
            List<weapon> Weapons = new List<weapon>();
            string input = "";
            string[] splitInput = input.Split(',');
            try
            {
                using (StreamReader sr = new StreamReader(FilePath))
                {
                    //Read the header line and discard.
                    sr.ReadLine();
                    input = sr.ReadLine();
                    while (input != null)
                    {
                        splitInput = input.Split(',');
                        //Console.WriteLine(splitInput[0]);
                        Weapons.Add(new weapon(splitInput[0], char.Parse(splitInput[1]), int.Parse(splitInput[2]), splitInput[3], int.Parse(splitInput[4]), int.Parse(splitInput[5]), int.Parse(splitInput[6]), int.Parse(splitInput[7]), float.Parse(splitInput[8]), float.Parse(splitInput[9]), float.Parse(splitInput[10]), float.Parse(splitInput[11]), float.Parse(splitInput[12]), int.Parse(splitInput[13]), int.Parse(splitInput[14]), float.Parse(splitInput[15]), int.Parse(splitInput[16]), float.Parse(splitInput[17]), float.Parse(splitInput[18]), int.Parse(splitInput[19]), float.Parse(splitInput[20]), int.Parse(splitInput[21]), int.Parse(splitInput[22]), int.Parse(splitInput[23]), int.Parse(splitInput[24])));
                        input = sr.ReadLine();
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Reading Weapons Failed.");
                Console.WriteLine(e.Message);
            }
            return Weapons;
        }
        public static Dictionary<string,weapon> ReadWeaponDictionary(string FilePath)
        {
            Dictionary<string, weapon> Dictionary = new Dictionary<string, weapon>();
            string input = "";
            string[] splitInput = input.Split(',');
            try
            {
                using (StreamReader sr = new StreamReader(FilePath))
                {
                    //Read the header line and discard
                    sr.ReadLine();
                    input = sr.ReadLine();
                    while (input != null)
                    {
                        splitInput = input.Split(',');
                        Dictionary.Add(splitInput[0] + " " + splitInput[1],new weapon(splitInput[0], char.Parse(splitInput[1]), int.Parse(splitInput[2]), splitInput[3], int.Parse(splitInput[4]), int.Parse(splitInput[5]), int.Parse(splitInput[6]), int.Parse(splitInput[7]), float.Parse(splitInput[8]), float.Parse(splitInput[9]), float.Parse(splitInput[10]), float.Parse(splitInput[11]), float.Parse(splitInput[12]), int.Parse(splitInput[13]), int.Parse(splitInput[14]), float.Parse(splitInput[15]), int.Parse(splitInput[16]), float.Parse(splitInput[17]), float.Parse(splitInput[18]), int.Parse(splitInput[19]), float.Parse(splitInput[20]), int.Parse(splitInput[21]), int.Parse(splitInput[22]), int.Parse(splitInput[23]), int.Parse(splitInput[24])));
                        //Console.WriteLine(Dictionary.Last());
                        input = sr.ReadLine();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Reading Weapons Failed.");
                Console.WriteLine(e.Message);
            }
            return Dictionary;
        }
        //Reads in list of shields and armor
        public static List<defense> ReadDefense(string FilePath)
        {
            List<defense> Defenses = new List<defense>();
            string input = "";
            string[] splitInput;
            try
            {
                using (StreamReader sr = new StreamReader(FilePath))
                {
                    //Read the header line and discard.
                    sr.ReadLine();
                    input = sr.ReadLine();
                    while (input != null)
                    {
                        splitInput = input.Split(',');
                        //Console.WriteLine(splitInput[0]);
                        Defenses.Add(new defense(splitInput[0], char.Parse(splitInput[1]), int.Parse(splitInput[2]), splitInput[3], int.Parse(splitInput[4]), int.Parse(splitInput[5]), int.Parse(splitInput[6]), int.Parse(splitInput[7]), float.Parse(splitInput[8])));
                        input = sr.ReadLine();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Reading Defenses Failed.");
                Console.WriteLine(e.Message);
            }
            return Defenses;
        }
        public static Dictionary<string,defense> ReadDefenseDictionary(string FilePath)
        {
            Dictionary<string, defense> Dictionary = new Dictionary<string, defense>();
            string input = "";
            string[] splitInput;
            try
            {
                using (StreamReader sr = new StreamReader(FilePath))
                {
                    //Read the header line and discard.
                    sr.ReadLine();
                    input = sr.ReadLine();
                    while (input != null)
                    {
                        splitInput = input.Split(',');
                        //Console.WriteLine(splitInput[0]);
                        Dictionary.Add(splitInput[0]+" "+splitInput[1],new defense(splitInput[0], char.Parse(splitInput[1]), int.Parse(splitInput[2]), splitInput[3], int.Parse(splitInput[4]), int.Parse(splitInput[5]), int.Parse(splitInput[6]), int.Parse(splitInput[7]), float.Parse(splitInput[8])));
                        input = sr.ReadLine();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Reading Defenses Failed.");
                Console.WriteLine(e.Message);
            }
            return Dictionary;
        }
        //Reads in list of Aux slots
        public static List<aux> ReadAux(string FilePath)
        {
            List<aux> Auxiliary = new List<aux>();
            string input = "";
            string[] splitInput;
            try
            {
                using (StreamReader sr = new StreamReader(FilePath))
                {
                    //Read the header line and discard.
                    sr.ReadLine();
                    input = sr.ReadLine();
                    while (input != null)
                    {
                        splitInput = input.Split(',');
                        //Console.WriteLine(splitInput[0]);
                        Auxiliary.Add(new aux(splitInput[0], char.Parse(splitInput[1]), int.Parse(splitInput[2]), splitInput[3], int.Parse(splitInput[4]), int.Parse(splitInput[5]), float.Parse(splitInput[6]), float.Parse(splitInput[7]),float.Parse(splitInput[8]),int.Parse(splitInput[9]),float.Parse(splitInput[10]),float.Parse(splitInput[11]),int.Parse(splitInput[12])));
                        input = sr.ReadLine();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Reading Auxiliary slots Failed.");
                Console.WriteLine(e.Message);
            }
            return Auxiliary;
        }
        public static Dictionary<string,aux> ReadAuxDictionary(string FilePath)
        {
            Dictionary<string, aux> Dictionary = new Dictionary<string, aux>();
            string input = "";
            string[] splitInput;
            try
            {
                using (StreamReader sr = new StreamReader(FilePath))
                {
                    //Read the header line and discard.
                    sr.ReadLine();
                    input = sr.ReadLine();
                    while (input != null)
                    {
                        splitInput = input.Split(',');
                        //Console.WriteLine(splitInput[0]);
                        Dictionary.Add(splitInput[0]+" "+splitInput[1],new aux(splitInput[0], char.Parse(splitInput[1]), int.Parse(splitInput[2]), splitInput[3], int.Parse(splitInput[4]), int.Parse(splitInput[5]), float.Parse(splitInput[6]), float.Parse(splitInput[7]), float.Parse(splitInput[8]), int.Parse(splitInput[9]), float.Parse(splitInput[10]), float.Parse(splitInput[11]), int.Parse(splitInput[12])));
                        input = sr.ReadLine();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Reading Auxiliary slots Failed.");
                Console.WriteLine(e.Message);
            }
            return Dictionary;
        }
        //Reads in list of Utility slots
        public static List<utility> ReadUtility(string FilePath)
        {
            List<utility> Utilities = new List<utility>();
            string input = "";
            string[] splitInput;
            try
            {
                using (StreamReader sr = new StreamReader(FilePath))
                {
                    //Read the header line and discard.
                    sr.ReadLine();
                    input = sr.ReadLine();
                    while (input != null)
                    {
                        splitInput = input.Split(',');
                        //Console.WriteLine(splitInput[0]);
                        Utilities.Add(new utility(splitInput[0],splitInput[1],splitInput[2],int.Parse(splitInput[3]),splitInput[4],int.Parse(splitInput[5]),int.Parse(splitInput[6]),float.Parse(splitInput[7]),int.Parse(splitInput[8]),int.Parse(splitInput[9]),int.Parse(splitInput[10]),float.Parse(splitInput[11]),int.Parse(splitInput[12]),float.Parse(splitInput[13])));
                        input = sr.ReadLine();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Reading Utilities Failed.");
                Console.WriteLine(e.Message);
            }
            return Utilities;
        }
        public static Dictionary<string,utility> ReadUtilityDictionary(string FilePath)
        {
            Dictionary<string, utility> Dictionary = new Dictionary<string, utility>();
            string input = "";
            string[] splitInput;
            try
            {
                using (StreamReader sr = new StreamReader(FilePath))
                {
                    //Read the header line and discard.
                    sr.ReadLine();
                    input = sr.ReadLine();
                    while (input != null)
                    {
                        splitInput = input.Split(',');
                        //Console.WriteLine(splitInput[0]);
                        string key = "";
                        if(splitInput[1] != "")
                        {
                            key = splitInput[1] + " " + splitInput[0];
                        }
                        else
                        {
                            key = splitInput[0];
                        }
                        Dictionary.Add(key,new utility(splitInput[0], splitInput[1], splitInput[2], int.Parse(splitInput[3]), splitInput[4], int.Parse(splitInput[5]), int.Parse(splitInput[6]), float.Parse(splitInput[7]), int.Parse(splitInput[8]), int.Parse(splitInput[9]), int.Parse(splitInput[10]), float.Parse(splitInput[11]), int.Parse(splitInput[12]), float.Parse(splitInput[13])));
                        input = sr.ReadLine();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Reading Utilities Failed.");
                Console.WriteLine(e.Message);
                Console.WriteLine(input);
            }
            return Dictionary;
        }
        //Writeout Results
        public static void WriteResults(List<ship> Ships, string FilePath)
        {
            string output = "";
            try
            {
                using (StreamWriter sr = new StreamWriter(FilePath))
                {
                    //Write the header
                    sr.WriteLine("Optimal Ships based on intelligence.");
                    //Foreach ship, write everything about them
                    foreach(ship Ship in Ships)
                    {
                        sr.WriteLine("Ship," + Ship.type);
                        output = "";
                        foreach(section Section in Ship.Sections)
                        {
                            sr.WriteLine("Section," + Section.name);
                            output = "Weapons,";
                            foreach(WeaponSlot Slot in Section.WeaponSlots)
                            {
                                output += Slot.weapon.name + ",";
                            }
                            sr.WriteLine(output);
                            output = "Defenses,";
                            foreach(DefenseSlot Slot in Section.DefenseSlots)
                            {
                                output += Slot.defense.name + ",";
                            }
                            foreach(AuxSlot Slot in Section.AuxSlots)
                            {
                                output += Slot.auxiliary.name + ",";
                            }
                            sr.WriteLine(output);
                        }
                        
                        output = "Utilities,";
                        try
                        {
                            output += Ship.thrusters.name + ",";
                        }
                        catch { }
                        try
                        {
                            output += Ship.sensors.name + ",";
                        }
                        catch { }
                        try
                        {
                            output += Ship.hyper_drive.name + ",";
                        }
                        catch { }
                        try
                        {
                            output += Ship.combat_computer.name + ",";
                        }
                        catch { }
                        try
                        {
                            output += Ship.reactor.name + ",";
                        }
                        catch { }
                        sr.WriteLine(output);
                        output = "Scores," + Ship.score[0] + "," + Ship.score[1] + "," + Ship.score[2];
                        sr.WriteLine(output);
                        sr.WriteLine();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Writing Results Failed.");
                Console.WriteLine(e.Message);
            }
        }

        //Ship creation wrapper that assigns it proper thrusters, etc.
        public static corvette CreateCorvette(int Section, utility Reactor, utility Thruster, utility Sensors, utility Hyper_Drive, utility Computer)
        {
            corvette Ship = new corvette(Section);
            Ship.reactor = Reactor;
            Ship.thrusters = Thruster;
            Ship.sensors = Sensors;
            Ship.hyper_drive = Hyper_Drive;
            Ship.combat_computer = Computer;
            //Ship.UpdateStats();
            return Ship;
        }
        public static destroyer CreateDestroyer(int BowSection, int SternSection, utility Reactor, utility Thruster, utility Sensors, utility Hyper_Drive, utility Computer)
        {
            destroyer Ship = new destroyer(BowSection, SternSection);
            Ship.reactor = Reactor;
            Ship.thrusters = Thruster;
            Ship.sensors = Sensors;
            Ship.hyper_drive = Hyper_Drive;
            Ship.combat_computer = Computer;
            //Ship.UpdateStats();
            return Ship;
        }

        //Compares My Ship to Their Ship, returning an array
        //Performance[0] is the overall ship performance (Defense / Attack)]
        //Performance[1] is the Attack score, defined as the number of time units required to kill their ship
        //Performance[2] is the Defense score, defined as the number of time units required for their ship to kill mine
        public static float[] ShipPerformance(ship MyShip, ship TheirShip)
        {
            float[] Performance = new float[3] { 0, 0, 0 };
            if (MyShip.isValid())
            {
                //Calculate Attack
                Performance[1] = AttackScore(MyShip, TheirShip);
                //Calculate Defense
                Performance[2] = AttackScore(TheirShip, MyShip);
                //Calculate Overall
                Performance[0] = Performance[2] / Performance[1];
            }
            else
            {
                Console.WriteLine("This ship isn't valid.");
            }
            return Performance;
        }

        //Calculates the 'attack' score of ship pair
        public static float AttackScore(ship MyShip, ship TheirShip)
        {
            float Score = 0f;
            //This shouldn't need to be done
            //Remove these if it runs slow
            TheirShip.UpdateStats();
            //MyShip.UpdateStats();
            //
            //Calculate target goals
            float shields = TheirShip.shield;
            float armor = TheirShip.armor;
            float hull = TheirShip.hull;
            float evasion = TheirShip.evasion;
            //Calculate base damage
            float damageShields = MyShip.WeaponShieldDamage(evasion, TheirShip.shield_regen);
            float damageArmor = MyShip.WeaponArmorDamage(evasion, TheirShip.armor_regen, true);
            float damageHull = MyShip.WeaponHullDamage(evasion, TheirShip.hull_regen, true, true);
            //Calculate base time - this is done separately in case of zeroes
            float timeHull = 10000;
            float timeArmor = 10000;
            float timeShields = 10000;
            if (damageHull > 0)
            {
                timeHull = hull / damageHull;
            }
            if (damageArmor > 0)
            {
                timeArmor = armor / damageArmor;
            }
            if (damageShields > 0)
            {
                timeShields = shields / damageShields;
            }
            //Console.WriteLine("Time to depletion are: " + timeShields + ", " + timeArmor + ", " + timeHull);
            //If the hull would die before anything else, just return the hull
            if (timeHull < timeArmor && timeHull < timeShields)
            {
                Score = timeHull;
                return Score;
            }
            //If the shields are depleted before the armor
            if (timeShields < timeArmor)
            {
                //Discount armor and hull for damage recieved during timeShields
                armor = armor - (damageArmor * timeShields);
                hull = hull - (damageHull * timeShields);
                //Recalculate damage to hull and armor based on no shields
                damageArmor = MyShip.WeaponArmorDamage(evasion, TheirShip.armor_regen, false);
                damageHull = MyShip.WeaponHullDamage(evasion, TheirShip.hull_regen, false, true);
                //time to depletion = timeShields + (discounted hull/armor / new damage rate)
                if (damageArmor > 0)
                {
                    timeArmor = timeShields + (armor / damageArmor);
                }
                if (damageHull > 0)
                {
                    timeHull = timeShields + (hull / damageHull);
                }
                //if hull depletes first, return the new hull depletion
                if (timeHull < timeArmor)
                {
                    Score = timeHull;
                    return Score;
                }
                //if armor depletes first
                else
                {
                    //Discount hull for damage recieved during new timeArmor
                    hull = hull - (damageHull * timeArmor);
                    //Recalculate damage to hull based on no shields or armor
                    damageHull = MyShip.WeaponHullDamage(evasion, TheirShip.hull_regen, false, false);
                    //time to depletion = newTimeArmor + (discounted hull / new damage rate)
                    if (damageHull > 0)
                    {
                        timeHull = timeArmor + (hull / damageHull);
                    }
                    else
                    {
                        //if we're not doing damage to hull here, we're screwed
                        timeHull = 10000;
                    }
                    //then return this value
                    Score = timeHull;
                    return Score;
                }
            }
            //Otherwise, armor is depleted first
            else
            {
                //Discount hull for damage recieved during timeArmor
                hull = hull - (damageHull * timeArmor);
                //Recalculate damage to hull for damage recieved during timeArmor (shields don't need to be recalculated)
                damageHull = MyShip.WeaponHullDamage(evasion, TheirShip.hull_regen, true, false);
                if (damageHull > 0)
                {
                    timeHull = timeArmor + (hull / damageHull);
                }
                //If timeHull < timeShields, return timeHull
                if (timeHull < timeShields)
                {
                    Score = timeHull;
                    return Score;
                }
                //Otherwise, discount hull for damage recieved during timeShields
                //We've allready accounted for damage recieved during timeArmor, so discount timeShields
                timeShields = timeShields - timeArmor;
                hull = hull - (damageHull * timeShields);
                //Recalculate damage to hull for damage recieved during timeShields
                damageHull = MyShip.WeaponHullDamage(evasion, TheirShip.hull_regen, false, false);
                if (damageHull > 0)
                {
                    timeHull = timeShields + (hull / damageHull);
                }
                else
                {
                    //If we aren't doing damage to hull when there is nothing left, we're screwed
                    timeHull = 10000;
                }
                Score = timeHull;
                return Score;
                //Return timeHull
                }

        }

        //Intelligence Ingestion
        public static List<BadGuy> LoadIntelligence(string FilePath, Dictionary<string,weapon> weaponDictionary, Dictionary<string,defense> defenseDictionary,Dictionary<string,aux> auxDictionary, Dictionary<string,utility> utilityDictionary)
        {
            List<BadGuy> Badguys = new List<BadGuy>();
            string input;
            string[] splitInput;
            string shipType = "";
            List<weapon> shipWeapons = new List<weapon>();
            List<defense> shipDefenses = new List<defense>();
            List<aux> shipAux = new List<aux>();
            List<utility> shipUtilities = new List<utility>();
            float shipWeight=1;
            try
            {
                using (StreamReader sr = new StreamReader(FilePath))
                {
                    input = sr.ReadLine();
                    while (input != null)
                    {
                        splitInput = input.Split(',');
                        if((splitInput[0] == "Bad Guy" || splitInput[0] == "Ship" || splitInput[0] == "End") && shipType != "")
                        {
                            //Console.WriteLine("Making a " + shipType + " ship! It will have " + shipWeapons.Count() + " weapons and " + shipDefenses.Count() + " defense and all of " + shipUtilities.Count() + " utilities.");
                            Badguys.Last().Ships.Add(new EnemyShip(shipType, shipWeapons, shipDefenses, shipAux, shipUtilities,shipWeight));
                            //Console.WriteLine("Ship made.");
                            shipType = "";
                            shipWeapons = new List<weapon>();
                            shipDefenses = new List<defense>();
                            shipUtilities = new List<utility>();
                            shipAux = new List<aux>();
                            shipWeight = 1;
                            
                        }
                        switch(splitInput[0])
                        {
                            case "Bad Guy":
                                {
                                    Badguys.Add(new BadGuy(splitInput[1]));
                                    Console.WriteLine("Making a bad guy named " + Badguys.Last().name);
                                    break;
                                }
                            case "Ship":
                                {
                                    shipType = splitInput[1];
                                    //Console.WriteLine("Ship type is now: " + shipType);
                                    break;
                                }
                            case "Weight":
                                {
                                    shipWeight = float.Parse(splitInput[1]);
                                    break;
                                }
                            case "Weapons":
                                {
                                    for(int i = 1; i<splitInput.Count();i++)
                                    {
                                        try
                                        {
                                            if (splitInput[i] != "")
                                            {
                                                shipWeapons.Add(weaponDictionary[splitInput[i]]);
                                                //Console.WriteLine("Adding a weapon: " + shipWeapons.Last().name);
                                            }
                                        }
                                        catch
                                        {
                                            Console.WriteLine("What the hell kind of weapon is a " + splitInput[i]);
                                        }
                                    }
                                    break;
                                }
                            case "Defenses":
                                {
                                    for(int i = 1; i < splitInput.Count(); i++)
                                    {
                                        try
                                        {
                                            if (splitInput[i] != "")
                                            {
                                                if (splitInput[i].Last() == 'A')
                                                {
                                                    shipAux.Add(auxDictionary[splitInput[i]]);
                                                }
                                                else
                                                {
                                                    shipDefenses.Add(defenseDictionary[splitInput[i]]);
                                                }
                                                //Console.WriteLine("Adding a defense: " + shipDefenses.Last().name);
                                            }
                                        }
                                        catch
                                        {
                                            Console.WriteLine("What the hell kind of defense is a " + splitInput[i]);
                                        }
                                    }
                                    break;
                                }
                            case "Utilities":
                                {
                                    for (int i = 1; i < splitInput.Count(); i++)
                                    {
                                        try
                                        {
                                            if (splitInput[i] != "")
                                            {
                                                shipUtilities.Add(utilityDictionary[splitInput[i]]);
                                                //Console.WriteLine("Adding a utility: " + shipUtilities.Last().name);
                                            }
                                        }
                                        catch
                                        {
                                            Console.WriteLine("What the hell kind of utility is a " + splitInput[i]);
                                        }
                                    }
                                    break;
                                }
                            case "End":
                                {
                                    //Don't do anything.
                                    break;
                                }
                        }
                        input = sr.ReadLine();
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Loading intelligence failed.");
                Console.WriteLine(e);
            }
            return Badguys;
        }

        //Tech tree ingestion
        public static Dictionary<string,int> LoadTech(string FilePath)
        {
            Dictionary<string, int> Tech = new Dictionary<string, int>();
            string input = "";
            string[] splitInput;
            try
            {
                using (StreamReader sr = new StreamReader(FilePath))
                {
                    //Read the header line and discard.
                    sr.ReadLine();
                    input = sr.ReadLine();
                    while (input != null)
                    {
                        splitInput = input.Split(',');
                        //Console.WriteLine(splitInput[0]);
                        string key = splitInput[0];
                        Tech.Add(key, int.Parse(splitInput[1]));
                        input = sr.ReadLine();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Reading Technologies Failed.");
                Console.WriteLine(e.Message);
                Console.WriteLine(input);
            }
            return Tech;
        }

        //Improved optimizers
        public static corvette ImpOptimizeCorvette(List<weapon> availableWeapons, List<defense> availableDefenses, List<aux> availableAux, List<utility> availableUtilities, List<BadGuy> BadGuys)
        {
            corvette Best = new corvette(1);
            corvette Working = new corvette(1);
            float[] BestScore = new float[3];
            //All available slots. Put into list so we can cycle it down in iterator
            List<Slot> Slots;
            //All available slots put into the SlotType list for cycling with enumerator
            List<SlotType> SlotTypes;
            //List of equipment that can go in each slot
            List<equipment>[] slotEquip;
            //Cycling through all of the section options..
            for (int i = 1; i < 4; i++)
            {
                Console.WriteLine("Testing corvette hull type: " + i);
                Working = new corvette(i);
                Slots = new List<Slot>();
                SlotTypes = new List<SlotType>();
                //Pull a total list of slots available, putting them into the respective slot types
                foreach (section Sect in Working.Sections)
                {
                    foreach (WeaponSlot Slot in Sect.WeaponSlots)
                    {
                        if (SlotTypes.Any(p => p.type == 'W' && p.size == Slot.size))
                        {
                            SlotTypes.Find(p => p.type == 'W' && p.size == Slot.size).number++;
                        }
                        else
                        {
                            SlotTypes.Add(new SlotType(Slot.size, 'W', 1));
                        }
                        //Slots.Add(Slot);
                    }
                    foreach (DefenseSlot Slot in Sect.DefenseSlots)
                    {
                        if (SlotTypes.Any(p => p.type == 'D' && p.size == Slot.size))
                        {
                            SlotTypes.Find(p => p.type == 'D' && p.size == Slot.size).number++;
                        }
                        else
                        {
                            SlotTypes.Add(new SlotType(Slot.size, 'D', 1));
                        }
                        //Slots.Add(Slot);
                    }
                    foreach (AuxSlot Slot in Sect.AuxSlots)
                    {
                        if (SlotTypes.Any(p => p.type == 'A' && p.size == Slot.size))
                        {
                            SlotTypes.Find(p => p.type == 'A' && p.size == Slot.size).number++;
                        }
                        else
                        {
                            SlotTypes.Add(new SlotType(Slot.size, 'A', 1));
                        }
                        //Slots.Add(Slot);
                    }
                }
                //See what fits in each slot
                slotEquip = new List<equipment>[Slots.Count];
                //Cycle through each slot
                foreach (SlotType Type in SlotTypes)
                {
                    //then look at the slot type and assign possible equipment to this slot
                    switch (Type.type)
                    {
                        case 'W':
                            {
                                foreach (weapon Weapon in availableWeapons)
                                {
                                    if (Weapon.size == Type.size)
                                    {
                                        Type.equipment.Add(Weapon);
                                    }
                                }
                                break;
                            }
                        case 'D':
                            {
                                foreach (defense Defense in availableDefenses)
                                {
                                    if (Defense.size == Type.size)
                                    {
                                        Type.equipment.Add(Defense);
                                    }
                                }
                                break;
                            }
                        case 'A':
                            {
                                foreach (aux Aux in availableAux)
                                {
                                    if (Aux.size == Type.size)
                                    {
                                        Type.equipment.Add(Aux);
                                    }
                                }
                                break;
                            }
                    }
                }
                SlotTypes[2].GenerateCombinations();
                //Now I have a list of slots and an array of equipment that fits in each slot.
                //I now want to iterate over that list and check to see if fits are valid and score them.
                Working = (corvette)Iterator2(availableUtilities, SlotTypes, Working, BadGuys);
                if (Working.score[0] > BestScore[0])
                {
                    Best = Working;
                    BestScore = Working.score;
                }
            }
            return Best;
        }
        public static platform ImpOptimizePlatform(List<weapon> availableWeapons, List<defense> availableDefenses, List<aux> availableAux, List<utility> availableUtilities, List<BadGuy> BadGuys)
        {
            platform Best = new platform(1, 1);
            platform Working = new platform(1, 1);
            float[] BestScore = new float[3];
            //All available slots. Put into list so we can cycle it down in iterator
            List<Slot> Slots;
            //All available slots put into the SlotType list for cycling with enumerator
            List<SlotType> SlotTypes;
            //List of equipment that can go in each slot
            List<equipment>[] slotEquip;
            //Cycling through all of the section options..
            for (int i = 1; i < 7; i++)
            {
                for (int a = i; a < 7; a++)
                {
                    Console.WriteLine("Testing platform hull type: " + i + ", " + a);
                    Working = new platform(i, a);
                    Slots = new List<Slot>();
                    SlotTypes = new List<SlotType>();
                    //Pull a total list of slots available, putting them into the respective slot types
                    foreach (section Sect in Working.Sections)
                    {
                        foreach (WeaponSlot Slot in Sect.WeaponSlots)
                        {
                            if (SlotTypes.Any(p => p.type == 'W' && p.size == Slot.size))
                            {
                                SlotTypes.Find(p => p.type == 'W' && p.size == Slot.size).number++;
                            }
                            else
                            {
                                SlotTypes.Add(new SlotType(Slot.size, 'W', 1));
                            }
                            //Slots.Add(Slot);
                        }
                        foreach (DefenseSlot Slot in Sect.DefenseSlots)
                        {
                            if(SlotTypes.Any(p => p.type == 'D' && p.size == Slot.size))
                            {
                                SlotTypes.Find(p => p.type == 'D' && p.size == Slot.size).number++;
                            }
                            else
                            {
                                SlotTypes.Add(new SlotType(Slot.size, 'D', 1));
                            }
                            //Slots.Add(Slot);
                        }
                        foreach (AuxSlot Slot in Sect.AuxSlots)
                        {
                            if(SlotTypes.Any(p=> p.type == 'A' && p.size == Slot.size))
                            {
                                SlotTypes.Find(p => p.type == 'A' && p.size == Slot.size).number++;
                            }
                            else
                            {
                                SlotTypes.Add(new SlotType(Slot.size, 'A', 1));
                            }
                            //Slots.Add(Slot);
                        }
                    }
                    //See what fits in each slot
                    slotEquip = new List<equipment>[Slots.Count];
                    //Cycle through each slot
                    foreach(SlotType Type in SlotTypes)
                    {
                        //then look at the slot type and assign possible equipment to this slot
                        switch (Type.type)
                        {
                            case 'W':
                                {
                                    foreach (weapon Weapon in availableWeapons)
                                    {
                                        if (Weapon.size == Type.size)
                                        {
                                            Type.equipment.Add(Weapon);
                                        }
                                    }
                                    break;
                                }
                            case 'D':
                                {
                                    foreach (defense Defense in availableDefenses)
                                    {
                                        if (Defense.size == Type.size)
                                        {
                                            Type.equipment.Add(Defense);
                                        }
                                    }
                                    break;
                                }
                            case 'A':
                                {
                                    foreach (aux Aux in availableAux)
                                    {
                                        if (Aux.size == Type.size)
                                        {
                                            Type.equipment.Add(Aux);
                                        }
                                    }
                                    break;
                                }
                        }
                    }
                    SlotTypes[2].GenerateCombinations();
                    //Now I have a list of slots and an array of equipment that fits in each slot.
                    //I now want to iterate over that list and check to see if fits are valid and score them.
                    Working = (platform)Iterator2(availableUtilities, SlotTypes, Working, BadGuys);
                    if (Working.score[0] > BestScore[0])
                    {
                        Best = Working;
                        BestScore = Working.score;
                    }
                }
            }
            return Best;
        }
        public static destroyer ImpOptimizeDestroyer(List<weapon> availableWeapons, List<defense> availableDefenses, List<aux> availableAux, List<utility> availableUtilities, List<BadGuy> BadGuys)
        {
            destroyer Best = new destroyer(1, 1);
            destroyer Working = new destroyer(1, 1);
            float[] BestScore = new float[3];
            //All available slots. Put into list so we can cycle it down in iterator
            List<Slot> Slots;
            //All available slots put into the SlotType list for cycling with enumerator
            List<SlotType> SlotTypes;
            //List of equipment that can go in each slot
            List<equipment>[] slotEquip;
            //Cycling through all of the section options..
            for (int i = 1; i < 4; i++)
            {
                for (int a = 1; a < 4; a++)
                {
                    Console.WriteLine("Testing destroyer hull type: " + i + ", " + a);
                    Working = new destroyer(i, a);
                    Slots = new List<Slot>();
                    SlotTypes = new List<SlotType>();
                    //Pull a total list of slots available, putting them into the respective slot types
                    foreach (section Sect in Working.Sections)
                    {
                        foreach (WeaponSlot Slot in Sect.WeaponSlots)
                        {
                            if (SlotTypes.Any(p => p.type == 'W' && p.size == Slot.size))
                            {
                                SlotTypes.Find(p => p.type == 'W' && p.size == Slot.size).number++;
                            }
                            else
                            {
                                SlotTypes.Add(new SlotType(Slot.size, 'W', 1));
                            }
                            //Slots.Add(Slot);
                        }
                        foreach (DefenseSlot Slot in Sect.DefenseSlots)
                        {
                            if (SlotTypes.Any(p => p.type == 'D' && p.size == Slot.size))
                            {
                                SlotTypes.Find(p => p.type == 'D' && p.size == Slot.size).number++;
                            }
                            else
                            {
                                SlotTypes.Add(new SlotType(Slot.size, 'D', 1));
                            }
                            //Slots.Add(Slot);
                        }
                        foreach (AuxSlot Slot in Sect.AuxSlots)
                        {
                            if (SlotTypes.Any(p => p.type == 'A' && p.size == Slot.size))
                            {
                                SlotTypes.Find(p => p.type == 'A' && p.size == Slot.size).number++;
                            }
                            else
                            {
                                SlotTypes.Add(new SlotType(Slot.size, 'A', 1));
                            }
                            //Slots.Add(Slot);
                        }
                    }
                    //See what fits in each slot
                    slotEquip = new List<equipment>[Slots.Count];
                    //Cycle through each slot
                    foreach (SlotType Type in SlotTypes)
                    {
                        //then look at the slot type and assign possible equipment to this slot
                        switch (Type.type)
                        {
                            case 'W':
                                {
                                    foreach (weapon Weapon in availableWeapons)
                                    {
                                        if (Weapon.size == Type.size)
                                        {
                                            Type.equipment.Add(Weapon);
                                        }
                                    }
                                    break;
                                }
                            case 'D':
                                {
                                    foreach (defense Defense in availableDefenses)
                                    {
                                        if (Defense.size == Type.size)
                                        {
                                            Type.equipment.Add(Defense);
                                        }
                                    }
                                    break;
                                }
                            case 'A':
                                {
                                    foreach (aux Aux in availableAux)
                                    {
                                        if (Aux.size == Type.size)
                                        {
                                            Type.equipment.Add(Aux);
                                        }
                                    }
                                    break;
                                }
                        }
                    }
                    SlotTypes[2].GenerateCombinations();
                    //Now I have a list of slots and an array of equipment that fits in each slot.
                    //I now want to iterate over that list and check to see if fits are valid and score them.
                    Working = (destroyer)Iterator2(availableUtilities, SlotTypes, Working, BadGuys);
                    if (Working.score[0] > BestScore[0])
                    {
                        Best = Working;
                        BestScore = Working.score;
                    }
                }
            }
            return Best;
        }
        public static cruiser ImpOptimizeCruiser(List<weapon> availableWeapons, List<defense> availableDefenses, List<aux> availableAux, List<utility> availableUtilities, List<BadGuy> BadGuys)
        {
            cruiser Best = new cruiser(1, 1, 1);
            cruiser Working = new cruiser(1, 1, 1);
            float[] BestScore = new float[3];
            //All available slots. Put into list so we can cycle it down in iterator
            List<Slot> Slots;
            //All available slots put into the SlotType list for cycling with enumerator
            List<SlotType> SlotTypes;
            //List of equipment that can go in each slot
            List<equipment>[] slotEquip;
            //Cycling through all of the section options..
            for (int i = 1; i < 4; i++)
            {
                for (int a = 1; a < 5; a++)
                {
                    for (int b = 1; b < 3; b++)
                    {
                        Console.WriteLine("Testing cruiser hull type: " + i + ", " + a + ", " + b);
                        Working = new cruiser(i, a, b);
                        Slots = new List<Slot>();
                        SlotTypes = new List<SlotType>();
                        //Pull a total list of slots available, putting them into the respective slot types
                        foreach (section Sect in Working.Sections)
                        {
                            foreach (WeaponSlot Slot in Sect.WeaponSlots)
                            {
                                if (SlotTypes.Any(p => p.type == 'W' && p.size == Slot.size))
                                {
                                    SlotTypes.Find(p => p.type == 'W' && p.size == Slot.size).number++;
                                }
                                else
                                {
                                    SlotTypes.Add(new SlotType(Slot.size, 'W', 1));
                                }
                                //Slots.Add(Slot);
                            }
                            foreach (DefenseSlot Slot in Sect.DefenseSlots)
                            {
                                if (SlotTypes.Any(p => p.type == 'D' && p.size == Slot.size))
                                {
                                    SlotTypes.Find(p => p.type == 'D' && p.size == Slot.size).number++;
                                }
                                else
                                {
                                    SlotTypes.Add(new SlotType(Slot.size, 'D', 1));
                                }
                                //Slots.Add(Slot);
                            }
                            foreach (AuxSlot Slot in Sect.AuxSlots)
                            {
                                if (SlotTypes.Any(p => p.type == 'A' && p.size == Slot.size))
                                {
                                    SlotTypes.Find(p => p.type == 'A' && p.size == Slot.size).number++;
                                }
                                else
                                {
                                    SlotTypes.Add(new SlotType(Slot.size, 'A', 1));
                                }
                                //Slots.Add(Slot);
                            }
                        }
                        //See what fits in each slot
                        slotEquip = new List<equipment>[Slots.Count];
                        //Cycle through each slot
                        foreach (SlotType Type in SlotTypes)
                        {
                            //then look at the slot type and assign possible equipment to this slot
                            switch (Type.type)
                            {
                                case 'W':
                                    {
                                        foreach (weapon Weapon in availableWeapons)
                                        {
                                            if (Weapon.size == Type.size)
                                            {
                                                Type.equipment.Add(Weapon);
                                            }
                                        }
                                        break;
                                    }
                                case 'D':
                                    {
                                        foreach (defense Defense in availableDefenses)
                                        {
                                            if (Defense.size == Type.size)
                                            {
                                                Type.equipment.Add(Defense);
                                            }
                                        }
                                        break;
                                    }
                                case 'A':
                                    {
                                        foreach (aux Aux in availableAux)
                                        {
                                            if (Aux.size == Type.size)
                                            {
                                                Type.equipment.Add(Aux);
                                            }
                                        }
                                        break;
                                    }
                            }
                        }
                        SlotTypes[2].GenerateCombinations();
                        //Now I have a list of slots and an array of equipment that fits in each slot.
                        //I now want to iterate over that list and check to see if fits are valid and score them.
                        Working = (cruiser)Iterator2(availableUtilities, SlotTypes, Working, BadGuys);
                        if (Working.score[0] > BestScore[0])
                        {
                            Best = Working;
                            BestScore = Working.score;
                        }
                    }
                }
            }
            return Best;
        }
        public static battleship ImpOptimizeBattleship(List<weapon> availableWeapons, List<defense> availableDefenses, List<aux> availableAux, List<utility> availableUtilities, List<BadGuy> BadGuys)
        {
            battleship Best = new battleship(1, 1, 1);
            battleship Working = new battleship(1, 1, 1);
            float[] BestScore = new float[3];
            //All available slots. Put into list so we can cycle it down in iterator
            List<Slot> Slots;
            //All available slots put into the SlotType list for cycling with enumerator
            List<SlotType> SlotTypes;
            //List of equipment that can go in each slot
            List<equipment>[] slotEquip;
            //Cycling through all of the section options..
            for (int i = 1; i < 5; i++)
            {
                for (int a = 1; a < 5; a++)
                {
                    for (int b = 1; b < 3; b++)
                    {
                        Console.WriteLine("Testing battleship hull type: " + i + ", " + a + ", " + b);
                        Working = new battleship(i, a, b);
                        Slots = new List<Slot>();
                        SlotTypes = new List<SlotType>();
                        //Pull a total list of slots available, putting them into the respective slot types
                        foreach (section Sect in Working.Sections)
                        {
                            foreach (WeaponSlot Slot in Sect.WeaponSlots)
                            {
                                if (SlotTypes.Any(p => p.type == 'W' && p.size == Slot.size))
                                {
                                    SlotTypes.Find(p => p.type == 'W' && p.size == Slot.size).number++;
                                }
                                else
                                {
                                    SlotTypes.Add(new SlotType(Slot.size, 'W', 1));
                                }
                                //Slots.Add(Slot);
                            }
                            foreach (DefenseSlot Slot in Sect.DefenseSlots)
                            {
                                if (SlotTypes.Any(p => p.type == 'D' && p.size == Slot.size))
                                {
                                    SlotTypes.Find(p => p.type == 'D' && p.size == Slot.size).number++;
                                }
                                else
                                {
                                    SlotTypes.Add(new SlotType(Slot.size, 'D', 1));
                                }
                                //Slots.Add(Slot);
                            }
                            foreach (AuxSlot Slot in Sect.AuxSlots)
                            {
                                if (SlotTypes.Any(p => p.type == 'A' && p.size == Slot.size))
                                {
                                    SlotTypes.Find(p => p.type == 'A' && p.size == Slot.size).number++;
                                }
                                else
                                {
                                    SlotTypes.Add(new SlotType(Slot.size, 'A', 1));
                                }
                                //Slots.Add(Slot);
                            }
                        }
                        //See what fits in each slot
                        slotEquip = new List<equipment>[Slots.Count];
                        //Cycle through each slot
                        foreach (SlotType Type in SlotTypes)
                        {
                            //then look at the slot type and assign possible equipment to this slot
                            switch (Type.type)
                            {
                                case 'W':
                                    {
                                        foreach (weapon Weapon in availableWeapons)
                                        {
                                            if (Weapon.size == Type.size)
                                            {
                                                Type.equipment.Add(Weapon);
                                            }
                                        }
                                        break;
                                    }
                                case 'D':
                                    {
                                        foreach (defense Defense in availableDefenses)
                                        {
                                            if (Defense.size == Type.size)
                                            {
                                                Type.equipment.Add(Defense);
                                            }
                                        }
                                        break;
                                    }
                                case 'A':
                                    {
                                        foreach (aux Aux in availableAux)
                                        {
                                            if (Aux.size == Type.size)
                                            {
                                                Type.equipment.Add(Aux);
                                            }
                                        }
                                        break;
                                    }
                            }
                        }
                        SlotTypes[2].GenerateCombinations();
                        //Now I have a list of slots and an array of equipment that fits in each slot.
                        //I now want to iterate over that list and check to see if fits are valid and score them.
                        Working = (battleship)Iterator2(availableUtilities, SlotTypes, Working, BadGuys);
                        if (Working.score[0] > BestScore[0])
                        {
                            Best = Working;
                            BestScore = Working.score;
                        }
                    }
                }
            }
            return Best;
        }
        public static titan ImpOptimizeTitan(List<weapon> availableWeapons, List<defense> availableDefenses, List<aux> availableAux, List<utility> availableUtilities, List<BadGuy> BadGuys)
        {
            titan Best = new titan();
            titan Working = new titan();
            float[] BestScore = new float[3];
            //All available slots. Put into list so we can cycle it down in iterator
            List<Slot> Slots;
            //All available slots put into the SlotType list for cycling with enumerator
            List<SlotType> SlotTypes;
            //List of equipment that can go in each slot
            List<equipment>[] slotEquip;
            //Cycling through all of the section options..
            Console.WriteLine("Testing titan hull type.");
            Working = new titan();
            Slots = new List<Slot>();
            SlotTypes = new List<SlotType>();
            //Pull a total list of slots available, putting them into the respective slot types
            foreach (section Sect in Working.Sections)
            {
                foreach (WeaponSlot Slot in Sect.WeaponSlots)
                {
                    if (SlotTypes.Any(p => p.type == 'W' && p.size == Slot.size))
                    {
                        SlotTypes.Find(p => p.type == 'W' && p.size == Slot.size).number++;
                    }
                    else
                    {
                        SlotTypes.Add(new SlotType(Slot.size, 'W', 1));
                    }
                    //Slots.Add(Slot);
                }
                foreach (DefenseSlot Slot in Sect.DefenseSlots)
                {
                    if (SlotTypes.Any(p => p.type == 'D' && p.size == Slot.size))
                    {
                        SlotTypes.Find(p => p.type == 'D' && p.size == Slot.size).number++;
                    }
                    else
                    {
                        SlotTypes.Add(new SlotType(Slot.size, 'D', 1));
                    }
                    //Slots.Add(Slot);
                }
                foreach (AuxSlot Slot in Sect.AuxSlots)
                {
                    if (SlotTypes.Any(p => p.type == 'A' && p.size == Slot.size))
                    {
                        SlotTypes.Find(p => p.type == 'A' && p.size == Slot.size).number++;
                    }
                    else
                    {
                        SlotTypes.Add(new SlotType(Slot.size, 'A', 1));
                    }
                    //Slots.Add(Slot);
                }
            }
            //See what fits in each slot
            slotEquip = new List<equipment>[Slots.Count];
            //Cycle through each slot
            foreach (SlotType Type in SlotTypes)
            {
                //then look at the slot type and assign possible equipment to this slot
                switch (Type.type)
                {
                    case 'W':
                        {
                            foreach (weapon Weapon in availableWeapons)
                            {
                                if (Weapon.size == Type.size)
                                {
                                    Type.equipment.Add(Weapon);
                                }
                            }
                            break;
                        }
                    case 'D':
                        {
                            foreach (defense Defense in availableDefenses)
                            {
                                if (Defense.size == Type.size)
                                {
                                    Type.equipment.Add(Defense);
                                }
                            }
                            break;
                        }
                    case 'A':
                        {
                            foreach (aux Aux in availableAux)
                            {
                                if (Aux.size == Type.size)
                                {
                                    Type.equipment.Add(Aux);
                                }
                            }
                            break;
                        }
                }
            }
            SlotTypes[2].GenerateCombinations();
            //Now I have a list of slots and an array of equipment that fits in each slot.
            //I now want to iterate over that list and check to see if fits are valid and score them.
            Working = (titan)Iterator2(availableUtilities, SlotTypes, Working, BadGuys);
            if (Working.score[0] > BestScore[0])
            {
                Best = Working;
                BestScore = Working.score;
            }
            return Best;
        }

        //Improved iterator that makes use of the enumerator GetPermutations()
        // Utilities = list of utilities I can use
        // slotEquip[] = list of equipment that can go into each slot
        public static ship Iterator2(List<utility> Utilities, List<SlotType> SlotTypes, ship Type, List<BadGuy> Badguys)
        {
            //My best ship is a new copy of the ship type I am considering, including current sections
            ship Best = newCopy(Type);
            //create my best score
            float[] BestScore;
            //If the blank type is valid, use it as my score. Otherwise, score everything at -10,000
            if (Best.isValid())
            {
                BestScore = Score(Best, Badguys);
            }
            else
            {
                BestScore = new float[] { -10000f, -10000f, -10000f };
            }
            //My working ship is a new copy of the type ship
            ship Working = newCopy(Type);
            //Create my working score
            float[] WorkingScore = new float[3];
            //Create my list of what is equipped and the maximum choices per array
            int[] equipArray = new int[SlotTypes.Count];
            int[] equipMax = new int[SlotTypes.Count];
            equipment[] SelectedEquipment;
            int counter = 0;
            int maximum = 1;
            for (int i = 0; i < equipMax.Count(); i++)
            {
                SlotTypes[i].GenerateCombinations();
                equipMax[i] = SlotTypes[i].combinations.Count();
                maximum = maximum * equipMax[i];
            }
            Console.WriteLine(maximum + " possible combinations.");  
            int Tenper = (int)Math.Floor(maximum / 10d);
            while (equipArray[0] < equipMax[0])
            {
                Working = newCopy(Type);
                WorkingScore = new float[3];
                //They need to make selected equipment the actual equipment
                SelectedEquipment = new equipment[] { };
                SelectedEquipment = Select(SlotTypes, equipArray);
                Working.AddLoadout(SelectedEquipment, Utilities);
                WorkingScore = Score(Working, Badguys);
                //Console.WriteLine("Best score: " + BestScore[0] + ", working score: " + WorkingScore[0] + " Best power: " + Best.power);
                if (WorkingScore[0] > BestScore[0])
                {
                    Best = Working;
                    BestScore = WorkingScore;
                }
                
                AddOne(equipArray, equipMax);
                counter++;
                if (counter % Tenper == 0)
                {
                    Console.WriteLine((counter / Tenper) * 10 + " % ");
                }
            }
            Best.score = BestScore;
            return Best;
        }
        public static ship newCopy(ship Ship)
        {
            ship newCopy;
            int hullLevel = Tech[Ship.type + " Hull"];
            if(hullLevel < 0)
            {
                hullLevel = 0;
            }
            else if(hullLevel > 2)
            {
                hullLevel = 2;
            }
            switch (Ship.type)
            {
                case "Corvette":
                    {
                        //Corvettes get +100 hull per excess tech level
                        newCopy = new corvette(Ship.sectionType);
                        newCopy.base_hull += (hullLevel * 100);
                        break;
                    }
                case "Destroyer":
                    {
                        //Destroyers get +200 hull per excess tech level
                        newCopy = new destroyer(Ship.sectionType);
                        newCopy.base_hull += (hullLevel * 200);
                        break;
                    }
                case "Cruiser":
                    {
                        //Cruisers get +400 hull per excess tech level
                        newCopy = new cruiser(Ship.sectionType);
                        newCopy.base_hull += (hullLevel * 400);
                        break;
                    }
                case "Battleship":
                    {
                        //Battleships get +800 hull per excess tech level
                        newCopy = new battleship(Ship.sectionType);
                        newCopy.base_hull += (hullLevel * 800);
                        break;
                    }
                case "Titan":
                    {
                        //Titans get +2000 per excess tech level
                        newCopy = new titan();
                        newCopy.base_hull += (hullLevel * 2000);
                        break;
                    }
                case "Platform":
                    {
                        //Platforms don't get any extra
                        newCopy = new platform(Ship.sectionType);
                        break;
                    }
                default:
                    {
                        Console.WriteLine("I don't know what a " + Ship.type + " is so I made a corvette.");
                        newCopy = new corvette(Ship.sectionType[0]);
                        break;
                    }
            }
            return newCopy;
        }
        public static int[] AddOne(int[] equipArray, int[] equipMax)
        {
            int[] final = new int[equipArray.Count()];
            int LastIndex = equipArray.Count() - 1;
            equipArray[LastIndex]++;
            for(int i = LastIndex; i > -1; i--)
            {
                if(equipArray[i] >= equipMax[i] && i > 0)
                {
                    equipArray[i] = 0;
                    equipArray[i - 1]++;
                    //for(int j = 0; j<equipArray.Count(); j++)
                    //{
                    //    Console.Write(equipArray[j] + " ");
                    //}
                    //Console.WriteLine();
                }
                if(i == 0 && equipArray[i] > equipMax[i])
                {
                    equipArray[i] = equipMax[i]+1;
                }
            }
            return final;
        }
        public static equipment[] Select(List<equipment>[] slotEquip, int[] equipArray)
        {
            equipment[] Equip = new equipment[slotEquip.Count()];
            for(int i = 0; i<slotEquip.Count();i++)
            {
                Equip[i] = slotEquip[i][equipArray[i]];
            }
            return Equip;
        }
        public static equipment[] Select(List<SlotType> SlotTypes, int[] equipArrays)
        {
            int totalequipment = 0;
            foreach(var Slot in SlotTypes)
            {
                totalequipment += Slot.number;
            }
            List<equipment> CurrentEquipment = new List<equipment>();
            List<equipment> SlotEquipment = new List<equipment>();
            //For each of the slot types
            for(int i = 0; i<SlotTypes.Count(); i++)
            {
                //The equipment array is equal to the corresponding value in the equipArray int[]
                int[] numEquip = SlotTypes[i].combinations[equipArrays[i]];
                SlotEquipment = SlotTypes[i].equipment;
                //For each piece of equipment
                for(int j = 0; j<SlotEquipment.Count(); j++)
                {
                    //If we're assigning more than one of these items
                    if(numEquip[j]>0)
                    {
                        //for numEquip[k] times..
                        for(int k = 0; k<numEquip[j]; k++)
                        {
                            //Add the equipment to the equipment!
                            CurrentEquipment.Add(SlotEquipment[j]);
                        }
                    }
                }
            }
            equipment[] Equip = CurrentEquipment.ToArray();
            return Equip;
        }

        //Scorer
        public static float[] Score(ship Ship, List<BadGuy> Badguys)
        {
            float[] AverageScore = new float[3];
            float[] Holder = new float[3];
            float ShipsChecked = 0;
            bool valid = Ship.isValid();
            if (valid)
            {
                //Console.WriteLine("Ships power is: " + Ship.power + " and I think this design is " + valid);
                foreach (BadGuy Baddie in Badguys)
                {
                    foreach (ship BadShip in Baddie.Ships)
                    {
                        Holder = ShipPerformance(Ship, BadShip);
                        AverageScore[0] += Holder[0] * BadShip.weight;
                        AverageScore[1] += Holder[1] * BadShip.weight;
                        AverageScore[2] += Holder[2] * BadShip.weight;
                        ShipsChecked += BadShip.weight;
                    }
                }
                AverageScore[0] = AverageScore[0] / ShipsChecked;
                AverageScore[1] = AverageScore[1] / ShipsChecked;
                AverageScore[2] = AverageScore[2] / ShipsChecked;
                Ship.score = AverageScore;
            }
            return AverageScore;
        }
    }

    //Have all weapons, defenses, and auxiliary inherit from equipment
    class equipment
    {
        public char Type;
        public string name;
    }
    // This class defines the basic ship weapons
    class weapon : equipment
    {
        //Size describes what slot it fits.
        // S = Small
        // M = Medium
        // L = Large
        // X = X-Large
        // T = Titan
        // P = Point defense
        // G = Gmissile
        // H = Hangar
        public char size;
        //Research tier of the weapon
        public int tier;
        //Type - Laser, mass driver, plasma, etc.
        public string tech_tree;
        // Cost in minerals
        public int cost;
        // Power cost
        public int power;
        // Minimum damage
        public int min_damage;
        // Maximum damage
        public int max_damage;
        // Multiplier for hull damage
        public float hull_damage;
        // Multiplier for shield damage
        public float shield_damage;
        // How much of damage bypasses shields
        public float shield_penetration;
        // Multiplier for armor damage
        public float armor_damage;
        // How much of damage bypasses armor
        public float armor_penetration;
        // ??
        public int min_windup;
        public int max_windup;
        // Time between shots
        public float cooldown;
        // Range
        public int range;
        //Figure out accuracy and tracking, they come up a looot
        public float accuracy;
        public float tracking;
        // Missile stats
        public int missile_speed;
        public float missile_evasion;
        public int missile_shield;
        public int missile_amor;
        public int missile_hull;
        // Max strike craft
        public int max_strike_craft;

        public weapon(string _name, char _size, int _tier, string _tech_tree, int _cost, int _power, int _min_damage, int _max_damage, float _hull_damage, float _shield_damage, float _shield_penetration, float _armor_damage, float _armor_penetration, int _min_windup, int _max_windup, float _cooldown, int _range, float _accuracy, float _tracking, int _missile_speed, float _missile_evasion, int _missile_shield, int _missile_amor, int _missile_hull, int _strike_craft)
        {
            this.name = _name;
            this.size = _size;
            this.tier = _tier;
            this.tech_tree = _tech_tree;
            this.cost = _cost;
            this.power = _power;
            this.min_damage = _min_damage;
            this.max_damage = _max_damage;
            this.hull_damage = _hull_damage;
            this.shield_damage = _shield_damage;
            this.shield_penetration = _shield_penetration;
            this.armor_damage = _armor_damage;
            this.armor_penetration = _armor_penetration;
            this.min_windup = _min_windup;
            this.max_windup = _max_windup;
            this.cooldown = _cooldown;
            this.range = _range;
            this.accuracy = _accuracy;
            this.tracking = _tracking;
            this.missile_speed = _missile_shield;
            this.missile_evasion = _missile_evasion;
            this.missile_shield = _missile_shield;
            this.missile_amor = _missile_amor;
            this.missile_hull = _missile_hull;
            this.max_strike_craft = _strike_craft;
            this.Type = 'W';
    }

    }
    // Either Shield or armor
    class defense : equipment
    {
        public char size;
        public int tier;
        public string tech_tree;
        public int cost;
        public int power;
        public int armor;
        public int shield;
        public float shield_regen;

        public defense(string _name, char _size, int _tier, string _tech_tree, int _cost, int _power, int _armor, int _shield, float _shield_regen)
        {
            this.name = _name;
            this.size = _size;
            this.tier = _tier;
            this.tech_tree = _tech_tree;
            this.cost = _cost;
            this.power = _power;
            this.armor = _armor;
            this.shield = _shield;
            this.shield_regen = _shield_regen;
            this.Type = 'D';
        }
    }
    // Auxilary slots
    class aux : equipment
    {
        public char size;
        public int tier;
        public string tech_tree;
        public int cost;
        public int power;
        public float shield_mult;
        public float hull_regen;
        public float armor_regen;
        public int accuracy;
        public float speed_mult;
        public float evasion_mult;
        public int tracking;

        public aux(string _name, char _size, int _tier, string _tech_tree, int _cost, int _power, float _shield_mult, float _hull_regen, float _armor_regen, int _accuracy, float _speed_mult, float _evasion_mult, int _tracking)
        {
            this.name = _name;
            this.size = _size;
            this.tier = _tier;
            this.tech_tree = _tech_tree;
            this.cost = _cost;
            this.power = _power;
            this.shield_mult = _shield_mult;
            this.hull_regen = _hull_regen;
            this.armor_regen = _armor_regen;
            this.accuracy = _accuracy;
            this.speed_mult = _speed_mult;
            this.evasion_mult = _evasion_mult;
            this.tracking = _tracking;
            this.Type = 'A';
        }
    }
    // Utilities - Reactor, FTL drive, sensors, etc.
    class utility : equipment
    {
        // Type of ship that can equip this
        public string ship;
        // Ship slot this fits in
        public string slot;
        public int tier;
        public string tech_tree;
        public int cost;
        public int power;
        public float ship_speed_mult;
        public int evasion;
        public int tracking;
        public int sensor_range;
        public float fire_rate_mult;
        public int accuracy;
        public float range_mult;

        public utility(string _name, string _ship, string _slot, int _tier, string _tech_tree, int _cost, int _power, float _ship_speed_mult, int _evasion, int _tracking, int _sensor_range, float _fire_rate_mult, int _accuracy, float _range_mult)
        {
            this.name = _name;
            this.ship = _ship;
            this.slot = _slot;
            this.tier = _tier;
            this.tech_tree = _tech_tree;
            this.cost = _cost;
            this.power = _power;
            this.ship_speed_mult = _ship_speed_mult;
            this.evasion = _evasion;
            this.tracking = _tracking;
            this.sensor_range = _sensor_range;
            this.fire_rate_mult = _fire_rate_mult;
            this.accuracy = _accuracy;
            this.range_mult = _range_mult;
        }
    }

    //A slot is a place for a weapon or utility
    class Slot
    {
        public char size;
        public char type;
    }
    class WeaponSlot : Slot
    {
        public weapon weapon;
        public WeaponSlot(char _size)
        {
            this.size = _size;
            this.weapon = new weapon("Nothing", size, 0, "Laser", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            this.type = 'W';
        }
        public void ClearSlot()
        {
            this.weapon = new weapon("Nothing", size, 0, "Laser", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }
    }
    class DefenseSlot : Slot
    {
        public defense defense;
        public DefenseSlot(char _size)
        {
            this.size = _size;
            this.defense = new defense("Nothing", size, 0, "Armor", 0, 0, 0, 0, 0);
            this.type = 'D';
        }
        public void ClearSlot()
        {
            this.defense = new defense("Nothing", size, 0, "Armor", 0, 0, 0, 0, 0);
        }
    }
    class AuxSlot : Slot
    {
        public aux auxiliary;
        public AuxSlot()
        {
            this.size = 'A';
            auxiliary = new aux("Nothing", size, 0, "Armor", 0, 0, 0, 0, 0, 0, 0, 0, 0);
            this.type = 'A';
        }
        public void ClearSlot()
        {
            auxiliary = new aux("Nothing", size, 0, "Armor", 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }
    }
    class SlotType
    {
        public char size;
        public char type;
        public int number;
        public List<equipment> equipment;
        public List<int[]> combinations;
        public SlotType(char _size, char _type, int _number)
        {
            this.size = _size;
            this.type = _type;
            this.number = _number;
            equipment = new List<equipment>();
        }
        public void GenerateCombinations()
        {
            List<int[]> Subcombos = new List<int[]>();
            int[] Input = new int[equipment.Count];
            combinations = CombinationGenerator(0, number, Subcombos, Input);
        }
        //Index is the equipment index, k is the number of remainint slots
        public List<int[]> CombinationGenerator(int index, int k, List<int[]> Subcombinations, int[] input)
        {
            if(input.Count() == 0)
            {
                return Subcombinations;
            }
            int[] output = new int[input.Count()];
            for(int i =0; i<input.Count();i++)
            {
                output[i] = input[i];
            }
            //If this is the last option on the equipment list, we have to fill the remaining slots with these
            if (index == (equipment.Count - 1))
            {
                output[index] = k;
                Subcombinations.Add(output);
                //for(int j = 0; j < index+1; j++)
                //{
                //    Console.Write(output[j] + ", ");
                //}
                //Console.WriteLine();
                return Subcombinations;
            }
            //If there are no more available slots left (k = 0), give it a value for this equipment and pass it on
            if (k == 0)
            {
                output[index] = 0;
                Subcombinations = CombinationGenerator(index + 1, 0, Subcombinations, output);
            }
            //Otherwise, we're working out way up!
            else
            {
                for (int v = 0; v < k + 1; v++)
                {
                    output[index] = v;
                    Subcombinations = CombinationGenerator(index + 1, (k - v), Subcombinations, output);
                }
            }

            return Subcombinations;
        }
    }

    //Section that goes on a ship
    class section
    {
        //Section type name
        public string name;
        //Weapon slots
        public List<WeaponSlot> WeaponSlots;
        //Defense slots
        public List<DefenseSlot> DefenseSlots;
        //Auxiliary slots
        public List<AuxSlot> AuxSlots;
        public section(char[] _WeaponSlots, char[] _UtilitySlots)
        {
            WeaponSlots = new List<WeaponSlot>();
            DefenseSlots = new List<DefenseSlot>();
            AuxSlots = new List<AuxSlot>();

            foreach(char Weapon in _WeaponSlots)
            {
                WeaponSlots.Add(new WeaponSlot(Weapon));
            }
            foreach(char Utility in _UtilitySlots)
            {
                if(Utility == 'A')
                {
                    AuxSlots.Add(new AuxSlot());
                }
                else
                {
                    DefenseSlots.Add(new DefenseSlot(Utility));
                }
            }
        }
    }

    //Bad guy
    class BadGuy
    {
        public string name;
        public List<ship> Ships;
        public BadGuy(string _name)
        {
            name = _name;
            Ships = new List<ship>();
        }
    }

    //Ship
    abstract class ship
    {
        public string type = "none";
        public List<section> Sections;
        public int[] sectionType;
        public string[] sectionName;
        public utility reactor;
        public utility thrusters;
        public utility sensors;
        public utility hyper_drive;
        public utility combat_computer;
        //Score
        public float[] score;
        //Weight is used for enemy ships, if you want to vary the likelihood of each ship appearing
        public float weight;

        //Baseline stats
        public int base_hull;
        public int base_armor;
        public int base_shield;
        public float base_evasion;
        public int base_cost;
        public int base_speed;
        public int sensor_range;
        //These base multipliers come up for guardians (leviathan expansion)
        public float base_range_mult;
        public float base_shield_mult;
        public float base_damage_mult;

        //Actual stats
        public int hull;
        public int armor;
        public int shield;
        private int Cost;
        public int cost
        {
            get { return Cost; }
            set
            {
                Cost = value;
                uptake_minerals = 0.01f * Cost;
                uptake_energy = 0.005f * Cost;
            }
        }
        public float evasion;
        public float evasion_mult;
        public int speed;
        public float speed_mult;
        public int power;
        //Used to compute excess power bonus
        public int power_required;
        public int power_supplied;
        public float exess_power_bonus;
        //
        public float uptake_minerals;
        public float uptake_energy;
        public float hull_regen;
        public float armor_regen;
        public float shield_regen;
        public float shield_mult;
        //Accuracy+Tracking+Fire_rate+Range_mult is carved out because it affects weapons, not ship
        public int accuracy_mod;
        public int tracking_mod;
        public float fire_rate_mult;
        public float range_mult;
        public float damage_mult;

        //This function will update all the stats based on the ship's equipment, etc.
        public void UpdateStats()
        {
            //Reset everything first
            hull = base_hull;
            cost = base_cost;
            power = 0;
            power_required = 0;
            power_supplied = 0;
            armor = base_armor;
            shield = base_shield;
            speed = base_speed;
            evasion = base_evasion;
            evasion_mult = 1f;
            speed_mult = 1f;
            hull_regen = 0f;
            armor_regen = 0f;
            shield_regen = 0f;
            if (base_shield_mult > 0)
            {
                shield_mult = 1f + base_shield_mult;
            }
            else
            {
                shield_mult = 1f;
            }
            accuracy_mod = 0;
            tracking_mod = 0;
            fire_rate_mult = 0f;
            if (base_range_mult > 0)
            {
                range_mult = 1f + base_range_mult;
            }
            else
            {
                range_mult = 1f;
            }
            if (base_damage_mult > 0)
            {
                damage_mult = 1f + base_damage_mult;
            }
            else
            {
                damage_mult = 1f;
            }
            //for each of the sections on the ship,

            foreach(section Sect in Sections)
            {
                //For each defense slot
                foreach(DefenseSlot Defense in Sect.DefenseSlots)
                {
                    cost += Defense.defense.cost;
                    power += Defense.defense.power;
                    armor += Defense.defense.armor;
                    shield += Defense.defense.shield;
                    shield_regen += Defense.defense.shield_regen;
                }
                //For each auxiliary slot
                foreach(AuxSlot Aux in Sect.AuxSlots)
                {
                    cost += Aux.auxiliary.cost;
                    power += Aux.auxiliary.power;
                    if(Aux.auxiliary.power > 0)
                    {
                        power_supplied += Aux.auxiliary.power;
                    }
                    shield_mult += Aux.auxiliary.shield_mult;
                    armor_regen += Aux.auxiliary.armor_regen;
                    hull_regen += Aux.auxiliary.hull_regen;
                    accuracy_mod += Aux.auxiliary.accuracy;
                    speed_mult += Aux.auxiliary.speed_mult;
                    evasion_mult += Aux.auxiliary.evasion_mult;
                    tracking_mod += Aux.auxiliary.tracking;
                }
                //For each weapon slot
                foreach(WeaponSlot Weapon in Sect.WeaponSlots)
                {
                    cost += Weapon.weapon.cost;
                    power += Weapon.weapon.power;
                }
            }
            //Reactor
            try
            {
                cost += reactor.cost;
                power += reactor.power;
                power_supplied += reactor.power;
            }
            catch
            {
                
            }
            //Thrusters
            try
            {
                cost += thrusters.cost;
                power += thrusters.power;
                speed_mult += thrusters.ship_speed_mult;
                evasion += thrusters.evasion;
            }
            catch { }
            //Sensors
            try
            {
                cost += sensors.cost;
                power += sensors.power;
                tracking_mod += sensors.tracking;
                sensor_range += sensors.sensor_range;
            }
            catch { }
            //Hyper drive
            try
            {
                cost += hyper_drive.cost;
                power += hyper_drive.power;
            }
            catch { }
            //Combat computer
            try
            {
                cost += combat_computer.cost;
                power += combat_computer.power;
                speed_mult += combat_computer.ship_speed_mult;
                evasion += combat_computer.evasion;
                tracking_mod += combat_computer.tracking;
                fire_rate_mult += combat_computer.fire_rate_mult;
                accuracy_mod += combat_computer.accuracy;
                range_mult += combat_computer.range_mult;
            }
            catch { }
            //Multipliers
            power_required = power_supplied - power;
            //Console.WriteLine("Power: " + power + ". Required Power: " + power_required + ". Power supplied: " + power_supplied);
            float excessMult = (1f - ((float)power_required / (float)power_supplied));
            //Console.WriteLine(excessMult);
            exess_power_bonus = 0.1f * excessMult;
            //Console.WriteLine("Excess Power Bonus: " + exess_power_bonus);
            if (exess_power_bonus > 0)
            {
                speed_mult += exess_power_bonus;
                evasion_mult += exess_power_bonus;
                damage_mult += exess_power_bonus;
            }
            speed = (int)(speed * speed_mult);
            shield = (int)(shield * shield_mult);
            evasion = (int)(evasion * evasion_mult);
        }
        //Determines if a ship's loadout is valid, aka if it has positive power
        public bool isValid()
        {
            UpdateStats();
            if(power>=0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //Add something onto the ship
        public void AddWeapon(weapon Weapon)
        {
            char Size = Weapon.size;
            bool Assigned = false;
            foreach (section Section in Sections)
            {
                foreach (WeaponSlot Slot in Section.WeaponSlots)
                {
                    if (Assigned == false)
                    {
                        if (Slot.size == Size && Slot.weapon.name == "Nothing")
                        {
                            Slot.weapon = Weapon;
                            Assigned = true;
                            //Console.WriteLine("I assigned this " + Weapon.name + " to the ship.");
                        }
                    }
                }
            }
            if (Assigned == false)
            {
                Console.WriteLine("I couldn't assign this weapon.");
            }
            else
            {
                //UpdateStats();
            }
        }
        public void AddDefense(defense Defense)
        {
            char Size = Defense.size;
            bool Assigned = false;
            foreach (section Section in Sections)
            {
                foreach (DefenseSlot Slot in Section.DefenseSlots)
                {
                    if (Assigned == false)
                    {
                        if (Slot.size == Size && Slot.defense.name == "Nothing")
                        {
                            Slot.defense = Defense;
                            Assigned = true;
                            //Console.WriteLine("I assigned this " + Defense.name + " to the ship.");
                        }
                    }
                }
            }
            if (Assigned == false)
            {
                Console.WriteLine("I couldn't assign this Defense.");
            }
            else
            {
                //UpdateStats();
            }
        }
        public void AddDefense(aux Auxiliary)
        {
            char Size = Auxiliary.size;
            bool Assigned = false;
            foreach (section Section in Sections)
            {
                foreach (AuxSlot Slot in Section.AuxSlots)
                {
                    if (Assigned == false)
                    {
                        if (Slot.size == Size && Slot.auxiliary.name == "Nothing")
                        {
                            Slot.auxiliary = Auxiliary;
                            Assigned = true;
                            //Console.WriteLine("I assigned this " + Auxiliary.name + " to the ship.");
                        }
                    }
                }
            }
            if (Assigned == false)
            {
                Console.WriteLine("I couldn't assign this Auxiliary.");
            }
            else
            {
                //UpdateStats();
            }
        }
        public void AddUtility(utility Utility)
        {
            if(Utility.ship != type && Utility.ship != "")
            {
                //Console.WriteLine("This " + Utility.name + " does not fit on a " + type);
            }
            else
            {
                switch (Utility.slot)
                {
                    case "Reactor":
                        {
                            reactor = Utility;
                            break;
                        }
                    case "Thruster":
                        {
                            thrusters = Utility;
                            break;
                        }
                    case "Sensor":
                        {
                            sensors = Utility;
                            break;
                        }
                    case "Hyper Drive":
                        {
                            hyper_drive = Utility;
                            break;
                        }
                    case "Computer":
                        {
                            combat_computer = Utility;
                            break;
                        }
                    default:
                        {
                            Console.WriteLine("Cannot assign " + Utility.name + " because it uses an unknown slot of " + Utility.slot);
                            break;
                        }
                }
                //Console.WriteLine("I assigned this " + Utility.name + " to this ship.");
                //UpdateStats();
            }
        }
        public void AddLoadout(List<weapon> Weapons, List<defense> Defenses, List<aux> Auxiliaries, List<utility> Utilities)
        {
            //Clear loadouts
            foreach (section Sect in Sections)
            {
                foreach (DefenseSlot Slot in Sect.DefenseSlots)
                {
                    Slot.ClearSlot();
                }
                foreach (WeaponSlot Slot in Sect.WeaponSlots)
                {
                    Slot.ClearSlot();
                }
                foreach (AuxSlot Slot in Sect.AuxSlots)
                {
                    Slot.ClearSlot();
                }
            }
            foreach (weapon Weapon in Weapons)
            {
                AddWeapon(Weapon);
            }
            foreach(defense Defense in Defenses)
            {
                AddDefense(Defense);
            }
            foreach(aux Aux in Auxiliaries)
            {
                AddDefense(Aux);
            }
            foreach(utility Utility in Utilities)
            {
                AddUtility(Utility);
            }
        }
        public void AddLoadout(equipment[] Equipment, List<utility> Utilities)
        {
            //Clear loadouts
            foreach(section Sect in Sections)
            {
                foreach(DefenseSlot Slot in Sect.DefenseSlots)
                {
                    Slot.ClearSlot();
                }
                foreach(WeaponSlot Slot in Sect.WeaponSlots)
                {
                    Slot.ClearSlot();
                }
                foreach(AuxSlot Slot in Sect.AuxSlots)
                {
                    Slot.ClearSlot();
                }
            }
            foreach (utility Utility in Utilities)
            {
                AddUtility(Utility);
            }
            for(int i = 0; i<Equipment.Count(); i++)
            {
                switch (Equipment[i].Type)
                {
                    case 'W':
                        {
                            AddWeapon((weapon)Equipment[i]);
                            break;
                        }
                    case 'D':
                        {
                            AddDefense((defense)Equipment[i]);
                            break;
                        }
                    case 'A':
                        {
                            AddDefense((aux)Equipment[i]);
                            break;
                        }
                }
            }
        }
        
        //Test the damage to a single ship
        public float WeaponShieldDamage(float TargetEvasion, float shieldRegen)
        {
            float Damage = 0;
            foreach(section Sect in Sections)
            {
                foreach(WeaponSlot slot in Sect.WeaponSlots)
                {
                    weapon Weapon = slot.weapon;
                    //Calculate this weapon's damage
                    float WeaponDamage = (Weapon.min_damage + Weapon.max_damage) / 2f;
                    //Console.WriteLine("Weapon " + Weapon.name + " has a damage of " + WeaponDamage + " and we have a weapon damage multiplier of " + damage_mult);
                    WeaponDamage = WeaponDamage * damage_mult;
                    //Then remove shield pen and multiply by shield damage!
                    WeaponDamage = WeaponDamage * (1 - Weapon.shield_penetration) * Weapon.shield_damage;
                    //Calculate this weapon's damage rate
                    //Console.WriteLine("Weapon " + Weapon.name + " has a cooldown of " + Weapon.cooldown + " and we have a fire rate multiplier of " + fire_rate_mult);
                    float FireRate = Weapon.cooldown * (1-fire_rate_mult);
                    float WeaponDamageRate = WeaponDamage / FireRate;
                    //Calculate hit chance
                    // Chance to hit = max(0, accuracy - max(0,evasion-tracking))
                    float Accuracy = Weapon.accuracy + accuracy_mod;
                    //Console.WriteLine("This weapon's accuracy is: " + Weapon.accuracy + " + " + accuracy_mod);
                    float Tracking = Weapon.tracking + tracking_mod;
                    //Console.WriteLine("This weapon's tracking is: " + Weapon.tracking + " + " + tracking_mod);
                    float HitChance = Math.Max(0, Accuracy - Math.Max(0, Math.Min(TargetEvasion,0.9f) - Tracking));
                    float finalDamage = WeaponDamageRate * HitChance;
                    //Console.WriteLine("Final damage for this " + Weapon.name + " is: " + finalDamage + " or: " + WeaponDamageRate + " * " +HitChance);
                    //Console.WriteLine("My accuracy is: " + Accuracy + " and my tracking is: " + Tracking + " and their evasion is: " + TargetEvasion);
                    Damage += finalDamage;
                }
            }
            Damage = Math.Max(0, Damage - shieldRegen);
            return Damage;
        }
        public float WeaponArmorDamage(float TargetEvasion, float armorRegen, bool Shields)
        {
            float Damage = 0;
            foreach (section Sect in Sections)
            {
                foreach (WeaponSlot slot in Sect.WeaponSlots)
                {
                    weapon Weapon = slot.weapon;
                    //Calculate this weapon's damage
                    float WeaponDamage = (Weapon.min_damage + Weapon.max_damage) / 2f;
                    //Console.WriteLine("Weapon " + Weapon.name + " has a damage of " + WeaponDamage + " and we have a weapon damage multiplier of " + damage_mult);
                    WeaponDamage = WeaponDamage * damage_mult;
                    //If shields are up, ignore discount for what doesn't penetrate shields.
                    if (Shields == true)
                    {
                        WeaponDamage = WeaponDamage * (Weapon.shield_penetration) * (1-Weapon.armor_penetration) * Weapon.armor_damage;
                    }
                    else
                    {
                        WeaponDamage = WeaponDamage * (1-Weapon.armor_penetration) * Weapon.armor_damage;
                    }
                    //Calculate this weapon's damage rate
                    //Console.WriteLine("Weapon " + Weapon.name + " has a cooldown of " + Weapon.cooldown + " and we have a fire rate multiplier of " + fire_rate_mult);
                    float FireRate = Weapon.cooldown * (1 - fire_rate_mult);
                    float WeaponDamageRate = WeaponDamage / FireRate;
                    //Calculate hit chance
                    // Chance to hit = max(0, accuracy - max(0,evasion-tracking))
                    float Accuracy = Weapon.accuracy + accuracy_mod;
                    //Console.WriteLine("This weapon's accuracy is: " + Weapon.accuracy + " + " + accuracy_mod);
                    float Tracking = Weapon.tracking + tracking_mod;
                    //Console.WriteLine("This weapon's tracking is: " + Weapon.tracking + " + " + tracking_mod);
                    float HitChance = Math.Max(0, Accuracy - Math.Max(0, Math.Min(TargetEvasion, 0.9f) - Tracking));
                    //Console.WriteLine("The final hit chance is: " + HitChance);
                    //string upadowna = "";
                    //if (Shields == true)
                    //{
                    //    upadowna = "up.";
                    //}
                    //else
                    //{
                    //    upadowna = "down.";
                    //}
                    //Console.WriteLine("The final weapon damage rate to armor for " + Weapon.name + " is: " + WeaponDamageRate * HitChance + " if shields are " + upadowna);
                    float finalDamage = WeaponDamageRate * HitChance;
                    Damage += finalDamage;
                }
            }
            Damage = Math.Max(0, Damage - armorRegen);
            return Damage;
        }
        public float WeaponHullDamage(float TargetEvasion, float hullRegen, bool Shields, bool Armor)
        {
            float Damage = 0;
            foreach (section Sect in Sections)
            {
                foreach (WeaponSlot slot in Sect.WeaponSlots)
                {
                    weapon Weapon = slot.weapon;
                    //Calculate this weapon's damage
                    float WeaponDamage = (Weapon.min_damage + Weapon.max_damage) / 2f;
                    //Console.WriteLine("Weapon " + Weapon.name + " has a damage of " + WeaponDamage + " and we have a weapon damage multiplier of " + damage_mult);
                    WeaponDamage = WeaponDamage * damage_mult;
                    //If shields are up, ignore discount for what doesn't penetrate shields.
                    if (Shields == true && Armor == true)
                    {
                        WeaponDamage = WeaponDamage * (Weapon.shield_penetration) * (Weapon.armor_penetration) * Weapon.hull_damage;
                    }
                    else if (Shields == true && Armor == false)
                    {
                        WeaponDamage = WeaponDamage * (Weapon.shield_penetration) * Weapon.hull_damage;
                    }
                    else if(Shields == false && Armor == true)
                    {
                        WeaponDamage = WeaponDamage * (Weapon.armor_penetration) * Weapon.hull_damage;
                    }
                    else if(Shields == false && Armor == false)
                    {
                        WeaponDamage = WeaponDamage * Weapon.hull_damage;
                    }
                    //Calculate this weapon's damage rate
                    //Console.WriteLine("Weapon " + Weapon.name + " has a cooldown of " + Weapon.cooldown + " and we have a fire rate multiplier of " + fire_rate_mult);
                    float FireRate = Weapon.cooldown * (1 - fire_rate_mult);
                    float WeaponDamageRate = WeaponDamage / FireRate;
                    //Calculate hit chance
                    // Chance to hit = max(0, accuracy - max(0,evasion-tracking))
                    float Accuracy = Weapon.accuracy + accuracy_mod;
                    //Console.WriteLine("This weapon's accuracy is: " + Weapon.accuracy + " + " + accuracy_mod);
                    float Tracking = Weapon.tracking + tracking_mod;
                    //Console.WriteLine("This weapon's tracking is: " + Weapon.tracking + " + " + tracking_mod);
                    float HitChance = Math.Max(0, Accuracy - Math.Max(0, Math.Min(TargetEvasion, 0.9f) - Tracking));
                    //Console.WriteLine("The final hit chance is: " + HitChance);
                    //string ShieldsUp = "";
                    //if(Shields == true)
                    //{
                    //    ShieldsUp = "up";
                    //}
                    //else
                    //{
                    //    ShieldsUp = "down";
                    //}
                    //string ArmorUp = "";
                    //if(Armor == true)
                    //{
                    //    ArmorUp = "up.";
                    //}
                    //else
                    //{
                    //    ArmorUp = "down.";
                    //}
                    //Console.WriteLine("The final weapon damage rate to hull for " + Weapon.name + " is: " + WeaponDamageRate * HitChance + " if shields are " + ShieldsUp + " and armor is " + ArmorUp);
                    float finalDamage = WeaponDamageRate * HitChance;
                    Damage += finalDamage;
                }
            }
            Damage = Math.Max(0, Damage - hullRegen);
            return Damage;
        }

        //Tell me what you have
        public void ReadOut()
        {
            UpdateStats();
            Console.WriteLine("Hello! I am a " + type);
            Console.Write("My weapons are: ");
            foreach(section Sect in Sections)
            {
                foreach(WeaponSlot Slot in Sect.WeaponSlots)
                {
                    Console.Write(Slot.weapon.name + " " + Slot.weapon.size + ", ");
                }
            }
            Console.WriteLine();
            Console.Write("My defenses are: ");
            foreach(section Sect in Sections)
            {
                foreach(DefenseSlot Slot in Sect.DefenseSlots)
                {
                    Console.Write(Slot.defense.name + " " + Slot.defense.size + ", ");
                }
                foreach(AuxSlot Slot in Sect.AuxSlots)
                {
                    Console.Write(Slot.auxiliary.name + " A, ");
                }
            }
            Console.WriteLine();
            Console.WriteLine("My overall stats are: Hull: " + hull + " Shields: " + shield + " Armor: " + armor + " Power: " + power);
            Console.WriteLine("My final score was: Overall: " + score[0] + " Attack: " + score[1] + " Defense: " + score[2]);
        }
    }

    //Because intel doesn't tell us what sections they use, we can't easily pull that data in
    //This ship framework allows us to just push in whatever it is they have onboard
    class EnemyShip : ship
    {
        public EnemyShip(string Type, List<weapon> Weapons, List<defense> Defenses, List<aux> Auxes, List<utility> Utilities)
        {
            weight = 1f;
            switch (Type)
            {
                case "Corvette":
                    {
                        type = "Corvette";
                        base_hull = 300;
                        base_cost = 60;
                        base_evasion = 0.6f;
                        base_speed = 170;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
                case "Destroyer":
                    {
                        type = "Destroyer";
                        base_hull = 800;
                        base_cost = 120;
                        base_evasion = 0.35f;
                        base_speed = 120;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
                case "Cruiser":
                    {
                        type = "Cruiser";
                        base_hull = 1400;
                        base_cost = 300;
                        base_evasion = 0.1f;
                        base_speed = 120;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
                case "Battleship":
                    {
                        type = "Battleship";
                        base_hull = 3000;
                        base_cost = 450;
                        base_evasion = 0.05f;
                        base_speed = 100;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
                case "Titan":
                    {
                        type = "Titan";
                        base_hull = 10000;
                        base_cost = 1200;
                        base_evasion = 0.05f;
                        base_speed = 100;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
                case "Platform":
                    {
                        type = "Platform";
                        base_hull = 1000;
                        base_cost = 0;
                        base_evasion = 0f;
                        base_speed = 6;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
                    //Guardians / Leviathans
                case "Ether Drake":
                    {
                        type = "Ether Drake";
                        base_hull = 150000;
                        base_cost = 0;
                        base_evasion = 0.25f;
                        base_speed = 160;
                        base_armor = 100000;
                        base_shield = 0;
                        break;
                    }
                case "Dimensional Horror":
                    {
                        type = "Dimensional Horror";
                        base_hull = 100000;
                        base_cost = 0;
                        base_evasion = -0.01f;
                        base_speed = 0;
                        base_armor = 100000;
                        base_shield = 100000;
                        base_range_mult = 0.5f;
                        break;
                    }
                case "Stellarite Devourer":
                    {
                        type = "Stellarite Devourer";
                        base_hull = 200000;
                        base_cost = 0;
                        base_evasion = 0.30f;
                        base_speed = 30;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
                case "Automated Dreadnought":
                    {
                        type = "Automated Dreadnought";
                        base_hull = 10000;
                        base_cost = 0;
                        base_evasion = 0.02f;
                        base_speed = 80;
                        base_armor = 5000;
                        base_shield = 0;
                        base_damage_mult = 2;
                        base_shield_mult = 3;
                        break;
                    }
                case "Technosphere":
                    {
                        type = "Technosphere";
                        base_hull = 50000;
                        base_cost = 0;
                        base_evasion = 0.35f;
                        base_speed = 80;
                        base_armor = 20000;
                        base_shield = 0;
                        break;
                    }
                    //Enigmatic fortressm
                case "Ancient Vault":
                    {
                        //Uses 11_guardians station_xl
                        type = "Ancient Vault";
                        base_hull = 50000;
                        base_cost = 0;
                        base_evasion = -0.01f;
                        base_speed = 0;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
                case "Ancient Guardian":
                    {
                        //Uses 11_guardians station_l
                        type = "Ancient Guardian";
                        base_hull = 20000;
                        base_cost = 0;
                        base_evasion = -0.01f;
                        base_speed = 0;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
                case "Ancient Warden":
                    {
                        //Uses 11_guardians station_m
                        type = "Ancient Warden";
                        base_hull = 10000;
                        base_cost = 0;
                        base_evasion = -0.01f;
                        base_speed = 0;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
                case "Ancient Defender":
                    {
                        //Uses 11_guardians station_s
                        type = "Ancient Defender";
                        base_hull = 5000;
                        base_cost = 0;
                        base_evasion = -0.01f;
                        base_speed = 0;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
                case "Ancient Sentinel":
                    {
                        //Uses 11_guardians station_xs
                        type = "Ancient Sentinel";
                        base_hull = 2500;
                        base_cost = 0;
                        base_evasion = -0.01f;
                        base_speed = 0;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
                    //Extradimensional crisis event
                case "Extradimensional Large":
                    {
                        type = "Extradimensional Large";
                        base_hull = 3000;
                        base_cost = 0;
                        base_evasion = 0.15f;
                        base_speed = 100;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
                case "Extradimensional Medium":
                    {
                        type = "Extradimensional Medium";
                        base_hull = 1500;
                        base_cost = 0;
                        base_evasion = 0.25f;
                        base_speed = 120;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
                case "Extradimensional Small":
                    {
                        type = "Extradimensional Small";
                        base_hull = 750;
                        base_cost = 0;
                        base_evasion = 0.2f;
                        base_speed = 140;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
                //Swarm crisis event
                case "Swarm Warrior": //Swarm large
                    {
                        type = "Swarm Warrior";
                        base_hull = 2000;
                        base_cost = 0;
                        base_evasion = 0.05f;
                        base_speed = 100;
                        base_armor = 3000;
                        base_shield = 0;
                        break;
                    }
                case "Swarm Brood Mother": //Carrier
                    {
                        type = "Swarm Brood Mother";
                        base_hull = 2000;
                        base_cost = 0;
                        base_evasion = 0.05f;
                        base_speed = 100;
                        base_armor = 3000;
                        base_shield = 0;
                        break;
                    }
                case "Swarm Small":
                    {
                        type = "Swarm Small";
                        base_hull = 500;
                        base_cost = 0;
                        base_evasion = 0.65f;
                        base_speed = 140;
                        base_armor = 1000;
                        base_shield = 0;
                        break;
                    }
                case "Swarm Queen":
                    {
                        type = "Swarm Queen";
                        base_hull = 4000;
                        base_cost = 0;
                        base_evasion = 0.05f;
                        base_speed = 100;
                        base_armor = 6000;
                        base_shield = 0;
                        break;
                    }
                case "Swarm Sentinel":
                    {
                        type = "Swarm Sentinel";
                        base_hull = 20000;
                        base_cost = 0;
                        base_evasion = 0.00f;
                        base_speed = 0;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
                //AI crisis event
                case "AI Large":
                    {
                        type = "AI Large";
                        base_hull = 2000;
                        base_cost = 0;
                        base_evasion = 0.10f;
                        base_speed = 100;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
                case "AI Small":
                    {
                        type = "AI Small";
                        base_hull = 800;
                        base_cost = 0;
                        base_evasion = 0.25f;
                        base_speed = 120;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
                default:
                    {
                        Console.WriteLine("I don't know what type of ship a " + Type + " is so I made it a battleship.");
                        type = "Battleship";
                        base_hull = 3000;
                        base_cost = 450;
                        base_evasion = 0.05f;
                        base_speed = 100;
                        base_armor = 0;
                        base_shield = 0;
                        break;
                    }
            }
            int weapons = Weapons.Count();
            int defenses = Defenses.Count();
            int aux = Auxes.Count() + defenses;
            char[] weaponSlots = new char[weapons];
            char[] defenseSlots = new char[aux];
            for(int i = 0; i<Weapons.Count; i++)
            {
                weaponSlots[i] = Weapons[i].size;
            }
            for(int i =0; i < aux; i++)
            {
                if (i < defenses)
                {
                    defenseSlots[i] = Defenses[i].size;
                }
                else
                {
                    //check this
                    defenseSlots[i] = Auxes[i - defenses].size;
                }
            }
            Sections = new List<section>();
            Sections.Add(new section(weaponSlots, defenseSlots));
            foreach (utility Utility in Utilities)
            {
                this.AddUtility(Utility);
            }
            foreach (weapon Weapon in Weapons)
            {
                this.AddWeapon(Weapon);
            }
            foreach(defense Defense in Defenses)
            {
                this.AddDefense(Defense);
            }
            foreach(aux Auxiliary in Auxes)
            {
                this.AddDefense(Auxiliary);
            }
            //if(Type == "Ether Drake")
            //{
            //    ReadOut();
            //}
        }
        public EnemyShip(string Type, List<weapon> Weapons, List<defense> Defenses, List<aux> Auxes, List<utility> Utilities, float _weight) : this(Type,Weapons,Defenses,Auxes,Utilities)
        {
            weight = _weight;
        }
    }

    class corvette : ship
    {
        //Three possible corvette sections:
        //1 - Interceptor
        //2 - Torpedo Boat
        //3 - Picket Ship
        public corvette(int Section)
        {
            type = "Corvette";
            base_hull = 300;
            base_cost = 60;
            base_evasion = 0.6f;
            base_speed = 170;
            base_armor = 0;
            base_shield = 0;
            Sections = new List<section>();
            sectionType = new int[1] { Section };
            switch(Section)
            {
                case 1:
                    {
                        //Interceptor section
                        sectionName = new string[] { "Interceptor" };
                        Sections.Add(new section(new char[] { 'S', 'S', 'S' }, new char[] { 'S', 'S', 'S', 'A' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                case 2:
                    {
                        //Torpedo boat section
                        sectionName = new string[] { "Torpedo" };
                        Sections.Add(new section(new char[] { 'S', 'G' }, new char[] { 'S', 'S', 'S', 'A' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                case 3:
                    {
                        //Picket Ship section
                        sectionName = new string[] { "Picket" };
                        Sections.Add(new section(new char[] { 'S', 'S', 'P' }, new char[] { 'S', 'S', 'S', 'A' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                default:
                    {
                        Console.WriteLine("I could not understand input value of " + Section + " so I gave this corvette an Interceptor section");
                        sectionType = new int[1] { 1 };
                        sectionName = new string[] { "Interceptor" };
                        Sections.Add(new section(new char[] { 'S', 'S', 'S' }, new char[] { 'S', 'S', 'S', 'A' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
            }
        }
        public corvette(int[] Section) : this(Section[0])
        {
            
        }
    }
    class destroyer : ship
    {
        //3 possible bow sections
        //1 - Artillery
        //2 - Gunship
        //3 - Picket Ship
        //3 possible stern sections
        //1 - Gunship
        //2 - Interceptor
        //3 - Picket Ship
        public destroyer(int BowSection, int SternSection)
        {
            type = "Destroyer";
            base_hull = 800;
            base_cost = 120;
            base_evasion = 0.35f;
            base_speed = 120;
            base_armor = 0;
            base_shield = 0;
            sectionType = new int[2] { BowSection, SternSection };
            sectionName = new string[2];
            Sections = new List<section>();
            switch (BowSection)
            {
                case 1:
                    {
                        //Artillery
                        sectionName[0] = "Artillery";
                        Sections.Add(new section(new char[] {'L'}, new char[] { 'S', 'S', 'S', 'S', 'S', 'S' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                case 2:
                    {
                        //Gunship.
                        sectionName[0] = "Gunship";
                        Sections.Add(new section(new char[] { 'S', 'S', 'M' }, new char[] { 'S', 'S', 'S', 'S', 'S', 'S' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                case 3:
                    {
                        //Picket
                        sectionName[0] = "Picket";
                        Sections.Add(new section(new char[] { 'S', 'S', 'P' }, new char[] { 'S', 'S', 'S', 'S', 'S', 'S' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                default:
                    {
                        Console.WriteLine("I could not understand bow section input of " + BowSection + " so I gave this destroyer a Gunship bow");
                        sectionName[0] = "Gunship";
                        sectionType[0] = 2;
                        Sections.Add(new section(new char[] { 'S', 'S', 'M' }, new char[] { 'S', 'S', 'S', 'S', 'S', 'S' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
            }
            switch (SternSection)
            {
                case 1:
                    {
                        //Gunship
                        sectionName[1] = "Gunship";
                        Sections.Add(new section(new char[] { 'M' }, new char[] { 'A' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
                case 2:
                    {
                        //Interceptor
                        sectionName[1] = "Interceptor";
                        Sections.Add(new section(new char[] { 'S', 'S' }, new char[] { 'A' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
                case 3:
                    {
                        //Picket Ship
                        sectionName[1] = "Picket";
                        Sections.Add(new section(new char[] { 'P', 'P' }, new char[] { 'A' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
                default:
                    {
                        Console.Write("I could not understand stern section input of " + SternSection + " so I gave this destroyer a Gunship stern");
                        sectionName[1] = "Gunship";
                        sectionType[1] = 1;
                        Sections.Add(new section(new char[] { 'M' }, new char[] { 'A' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
            }

        }
        public destroyer(int[] Sections) : this(Sections[0],Sections[1])
        {

        }
    }
    class cruiser : ship
    {
        //3 possible bow sections
        //1 - Artillery
        //2 - Broadside
        //3 - Torpedo
        //4 possible core sections
        //1 - Artillery
        //2 - Broadside
        //3 - Hangar
        //4 - Torpedo
        //2 possible stern sections
        //1 - Broadside
        //2 - Gunship
        public cruiser(int BowSection, int CoreSection, int SternSection)
        {
            type = "Cruiser";
            base_hull = 1400;
            base_cost = 300;
            base_evasion = 0.1f;
            base_speed = 120;
            base_armor = 0;
            base_shield = 0;
            sectionType = new int[3] { BowSection, CoreSection, SternSection };
            sectionName = new string[3];
            Sections = new List<section>();
            switch (BowSection)
            {
                case 1:
                    {
                        //Artillery
                        sectionName[0] = "Artillery";
                        Sections.Add(new section(new char[] { 'L' }, new char[] { 'M', 'M', 'M', 'M' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                case 2:
                    {
                        //Broadside
                        sectionName[0] = "Broadside";
                        Sections.Add(new section(new char[] { 'M', 'M' }, new char[] { 'M', 'M', 'M', 'M' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                case 3:
                    {
                        //Torpedo
                        sectionName[0] = "Torpedo";
                        Sections.Add(new section(new char[] { 'G', 'S', 'S' }, new char[] { 'M', 'M', 'M', 'M' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                default:
                    {
                        Console.WriteLine("I could not understand bow section input of " + BowSection + " so I gave this cruiser a Broadside bow");
                        sectionName[0] = "Broadside";
                        sectionType[0] = 2;
                        Sections.Add(new section(new char[] { 'M', 'M' }, new char[] { 'M', 'M', 'M', 'M' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
            }
            switch (CoreSection)
            {
                case 1:
                    {
                        //Artillery
                        sectionName[1] = "Artillery";
                        Sections.Add(new section(new char[] { 'M', 'L' }, new char[] { 'M', 'M', 'M', 'M' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
                case 2:
                    {
                        //Broadside
                        sectionName[1] = "Broadside";
                        Sections.Add(new section(new char[] { 'M', 'M', 'M' }, new char[] { 'M', 'M', 'M', 'M' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
                case 3:
                    {
                        //Hangar
                        sectionName[1] = "Hangar";
                        Sections.Add(new section(new char[] { 'P', 'P', 'H' }, new char[] { 'M', 'M', 'M', 'M' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
                case 4:
                    {
                        //Torpedo
                        sectionName[1] = "Torpedo";
                        Sections.Add(new section(new char[] { 'S', 'S', 'G' }, new char[] { 'M', 'M', 'M', 'M' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
                default:
                    {
                        Console.WriteLine("I could not understand the core section input of " + CoreSection + " I gave this cruiser a Broadside core.");
                        sectionName[1] = "Broadside";
                        sectionType[1] = 2;
                        Sections.Add(new section(new char[] { 'M', 'M', 'M' }, new char[] { 'M', 'M', 'M', 'M' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
            }
            switch (SternSection)
            {
                case 1:
                    {
                        //Broadside
                        sectionName[2] = "Broadside";
                        Sections.Add(new section(new char[] { 'M' }, new char[] { 'A', 'A' }));
                        Sections.Last().name = sectionName[2];
                        break;
                    }
                case 2:
                    {
                        //Gunship
                        sectionName[2] = "Gunship";
                        Sections.Add(new section(new char[] { 'S', 'S' }, new char[] { 'A', 'A' }));
                        Sections.Last().name = sectionName[2];
                        break;
                    }
                default:
                    {
                        Console.Write("I could not understand stern section input of " + SternSection + " so I gave this cruiser a Gunship stern");
                        sectionName[2] = "Broadside";
                        sectionType[2] = 2;
                        Sections.Add(new section(new char[] { 'S', 'S' }, new char[] { 'A', 'A' }));
                        Sections.Last().name = sectionName[2];
                        break;
                    }
            }

        }
        public cruiser(int[] Sections) : this(Sections[0], Sections[1], Sections[2])
        {

        }
    }
    class battleship : ship
    {
        //4 possible bow sections
        //1 - Artillery
        //2 - Broadside
        //3 - Hangar
        //4 - Spinal Mount
        //4 possible core sections
        //1 - Artillery
        //2 - Broadside
        //3 - Carrier
        //4 - Hangar
        //2 possible stern sections
        //1 - Artillery
        //2 - Broadside
        public battleship(int BowSection, int CoreSection, int SternSection)
        {
            type = "Battleship";
            base_hull = 3000;
            base_cost = 450;
            base_evasion = 0.05f;
            base_speed = 100;
            base_armor = 0;
            base_shield = 0;
            sectionType = new int[3] { BowSection, CoreSection, SternSection };
            sectionName = new string[3];
            Sections = new List<section>();
            switch (BowSection)
            {
                case 1:
                    {
                        //Artillery
                        sectionName[0] = "Artillery";
                        Sections.Add(new section(new char[] { 'L', 'L' }, new char[] { 'L', 'L', 'L' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                case 2:
                    {
                        //Broadside
                        sectionName[0] = "Broadside";
                        Sections.Add(new section(new char[] { 'S', 'S', 'M', 'L' }, new char[] { 'L', 'L', 'L' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                case 3:
                    {
                        //Hangar
                        sectionName[0] = "Hangar";
                        Sections.Add(new section(new char[] { 'M', 'P', 'P', 'H' }, new char[] { 'L', 'L', 'L' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                case 4:
                    {
                        //Spinal Mount
                        sectionName[0] = "Spinal Mount";
                        Sections.Add(new section(new char[] { 'X' }, new char[] { 'L', 'L', 'L' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                default:
                    {
                        Console.WriteLine("I could not understand bow section input of " + BowSection + " so I gave this battleship a Broadside bow");
                        sectionName[0] = "Broadside";
                        sectionType[0] = 2;
                        Sections.Add(new section(new char[] { 'S', 'S', 'M', 'L' }, new char[] { 'L', 'L', 'L' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
            }
            switch (CoreSection)
            {
                case 1:
                    {
                        //Artillery
                        sectionName[1] = "Artillery";
                        Sections.Add(new section(new char[] { 'L', 'L', 'L' }, new char[] { 'L', 'L', 'L' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
                case 2:
                    {
                        //Broadside
                        sectionName[1] = "Broadside";
                        Sections.Add(new section(new char[] { 'M', 'M', 'L', 'L' }, new char[] { 'L', 'L', 'L' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
                case 3:
                    {
                        //Carrier
                        sectionName[1] = "Carrier";
                        Sections.Add(new section(new char[] { 'S', 'S', 'P' , 'P', 'H', 'H' }, new char[] { 'L', 'L', 'L' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
                case 4:
                    {
                        //Hangar
                        sectionName[1] = "Hangar";
                        Sections.Add(new section(new char[] { 'M', 'M', 'M', 'M', 'H' }, new char[] { 'L', 'L', 'L' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
                default:
                    {
                        Console.WriteLine("I could not understand the core section input of " + CoreSection + " I gave this battleship a Broadside core.");
                        sectionName[1] = "Broadside";
                        sectionType[1] = 2;
                        Sections.Add(new section(new char[] { 'M', 'M', 'L', 'L' }, new char[] { 'L', 'L', 'L' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
            }
            switch (SternSection)
            {
                case 1:
                    {
                        //Artillery
                        sectionName[2] = "Artillery";
                        Sections.Add(new section(new char[] { 'L' }, new char[] { 'A', 'A' }));
                        Sections.Last().name = sectionName[2];
                        break;
                    }
                case 2:
                    {
                        //Broadside
                        sectionName[2] = "Broadside";
                        Sections.Add(new section(new char[] { 'M', 'M' }, new char[] { 'A', 'A' }));
                        Sections.Last().name = sectionName[2];
                        break;
                    }
                default:
                    {
                        Console.Write("I could not understand stern section input of " + SternSection + " so I gave this battleship a Gunship stern");
                        sectionName[2] = "Broadside";
                        sectionType[2] = 2;
                        Sections.Add(new section(new char[] { 'M', 'M' }, new char[] { 'A', 'A' }));
                        Sections.Last().name = sectionName[2];
                        break;
                    }
            }
        }
        public battleship(int[] Sections) : this(Sections[0], Sections[1], Sections[2])
        {

        }
    }
    class titan : ship
    {
        public titan()
        {
            type = "Titan";
            base_hull = 10000;
            base_cost = 1200;
            base_evasion = 0.05f;
            base_speed = 100;
            base_armor = 0;
            base_shield = 0;
            sectionType = new int[3] { 1, 1, 1 };
            sectionName = new string[3] { "Titan Bow", "Titan Core", "Titan Stern" };
            Sections = new List<section>();
            Sections.Add(new section(new char[] { 'T' }, new char[] { 'L', 'L', 'L', 'L', 'L', 'L' }));
            Sections.Last().name = sectionName[0];
            Sections.Add(new section(new char[] { 'L', 'L', 'L', 'L' }, new char[] { 'L', 'L', 'L', 'L', 'L', 'L' }));
            Sections.Last().name = sectionName[1];
            Sections.Add(new section(new char[] { 'L', 'L', 'L' }, new char[] { 'A', 'A', 'A' }));
            Sections.Last().name = sectionName[2];
        }
        public titan(int[] Sections) : this()
        {

        }
    }
    class platform : ship
    {
        //Both sections have the same options
        //1 - Light
        //2 - Medium
        //3 - Heavy
        //4 - Point defense
        //5 - Missile
        //6 - Hangar
        public platform(int Section1, int Section2)
        {
            type = "Platform";
            base_hull = 1000;
            base_cost = 0;
            base_evasion = 0f;
            base_speed = 6;
            base_armor = 0;
            base_shield = 0;
            sectionType = new int[2] { Section1, Section2 };
            sectionName = new string[2];
            Sections = new List<section>();
            switch(Section1)
            {
                case 1:
                    {
                        //Light
                        sectionName[0] = "Light";
                        Sections.Add(new section(new char[] { 'S', 'S', 'S', 'S' }, new char[] { 'M', 'M', 'M', 'A' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                case 2:
                    {
                        //Medium
                        sectionName[0] = "Medium";
                        Sections.Add(new section(new char[] { 'M', 'M' }, new char[] { 'M', 'M', 'M', 'A' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                case 3:
                    {
                        //Heavy
                        sectionName[0] = "Heavy";
                        Sections.Add(new section(new char[] { 'L' }, new char[] { 'M', 'M', 'M', 'A' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                case 4:
                    {
                        //Point defense
                        sectionName[0] = "Point Defense";
                        Sections.Add(new section(new char[] { 'P', 'P', 'P', 'P' }, new char[] { 'M', 'M', 'M', 'A' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                case 5:
                    {
                        //Missile
                        sectionName[0] = "Missile";
                        Sections.Add(new section(new char[] { 'G', 'G' }, new char[] { 'M', 'M', 'M', 'A' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                case 6:
                    {
                        //Hangar
                        sectionName[0] = "Hangar";
                        Sections.Add(new section(new char[] { 'H' }, new char[] { 'M', 'M', 'M', 'A' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
                default:
                    {
                        Console.WriteLine("I couldn't understand Section1 input of " + Section1 + " so I gave this platform a medium section.");
                        sectionName[0] = "Medium";
                        sectionType[0] = 2;
                        Sections.Add(new section(new char[] { 'M', 'M' }, new char[] { 'M', 'M', 'M', 'A' }));
                        Sections.Last().name = sectionName[0];
                        break;
                    }
            }
            switch(Section2)
            {
                case 1:
                    {
                        //Light
                        sectionName[1] = "Light";
                        Sections.Add(new section(new char[] { 'S', 'S', 'S', 'S' }, new char[] { 'M', 'M', 'M', 'A' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
                case 2:
                    {
                        //Medium
                        sectionName[1] = "Medium";
                        Sections.Add(new section(new char[] { 'M', 'M' }, new char[] { 'M', 'M', 'M', 'A' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
                case 3:
                    {
                        //Heavy
                        sectionName[1] = "Heavy";
                        Sections.Add(new section(new char[] { 'L' }, new char[] { 'M', 'M', 'M', 'A' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
                case 4:
                    {
                        //Point defense
                        sectionName[1] = "Point Defense";
                        Sections.Add(new section(new char[] { 'P', 'P', 'P', 'P' }, new char[] { 'M', 'M', 'M', 'A' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
                case 5:
                    {
                        //Missile
                        sectionName[1] = "Missile";
                        Sections.Add(new section(new char[] { 'G', 'G' }, new char[] { 'M', 'M', 'M', 'A' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
                case 6:
                    {
                        //Hangar
                        sectionName[1] = "Hangar";
                        Sections.Add(new section(new char[] { 'H' }, new char[] { 'M', 'M', 'M', 'A' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
                default:
                    {
                        Console.WriteLine("I couldn't understand Section1 input of " + Section1 + " so I gave this platform a medium section.");
                        sectionName[1] = "Medium";
                        sectionType[1] = 2;
                        Sections.Add(new section(new char[] { 'M', 'M' }, new char[] { 'M', 'M', 'M', 'A' }));
                        Sections.Last().name = sectionName[1];
                        break;
                    }
            }

        }
        public platform(int[] Sections) : this(Sections[0], Sections[1])
        {

        }
    }
}
