# Stellaris-Ship-Optimizer
<b>This program 'optimizes' a Stellaris ship based on enemy intelligence.</b>

Basically, this program pits a ship against the ships of your enemy and calculates:

a. The time it takes for your ship to kill their ship (Attack score, Lower is better)

b. The time it takes their ship to kill your ship (Defense Score, Higher is better)

c. A final score equal to Defense Score / Attack Score. i.e. how many times longer it takes to be killed than to kill

The optimizer tests all possible combinations of weapons and defenses (without repeating any!) and outputs the best performing ships


<b>The optimizer doesn't consider a few things:</b>

a. Range, speed, disengagement - scoring assumes both ships start within range of eachother and they fight to the death

b. Installed combat computer - Since it doesn't consider any of the above, you'll have to choose the appropriate computer based on weapons

c. Point defense (sorta) - The optimizer has trouble with point defense and strike craft, because of (a).


<b>The optimizer does do some things you can't:</b>

a. It tracks damage to shield, armor, and hull separately allowing it to better assess the importance of shield and armor penetration or damage multipliers.

b. Fire rate augmentations are included in all calculations, making auxiliary fire control better than face value


<b>To run this program:</b>

Download the following files:

1. auxiliaries.csv - this has the data on all the ship auxiliaries

2. defenses.csv - this has the data on all the ship defenses

3. weapons.csv - this has the data on all of the ship weapons

4. utilities.csv - this has data on all the utilities, including reactors, thrusters, etc.

5. tech.csv - this is the csv where you'll put your current technology

6. intelligence.csv - this is your intelligence on one or more enemies. Currently has some data on a devouring swarm that was harassing me

7. Optimizer v1 - this folder has the install file for the optimizer. Download and install!

8. (Optional) intelligenceBuilder.xlsx - this excel file is the intelligence file as an excel sheet with the permissible values in dropdown menus. Included simply for ease of use. Make sure you save your intelligence as 'intelligence.csv' after you use it.


<b>To Run:</b>

1. Update the tech.csv to reflect your techs in game

2. Update the intelligence.csv to match what you know about your enemies. Use the intelligenceBuilder or refer to the 'permissableValues' for what you should call weapons, defenses, etc. NOTE THAT THE OPTIMIZER IS DUMB, IT CAN ONLY HANDLE ENTRIES THAT ARE EXACT.

3. Install the optimizer

4. Run the installer - it will prompt you for the folder with all of the csv files.

5. Check out the results when it finishes!

