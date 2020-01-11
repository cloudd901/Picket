# Picket
Plugin-based random ticket picker.
Use the pre-installed plugins or create your own based on their Github code.

![Icon](https://github.com/cloudd901/Picket/blob/master/Picket/picket.png)

Work-in-progress...


<h4>Random Pick</h4>
Clicking the ‘Random Pick’ button will activate the plugin animation. While the interface between the plugins are the same, each plugin is fundamentally different.

<h4>Plugins</h4>
More in-depth details on how each plugin works can be found on each of the plugin download pages.

The MagicWheel Plugin
- The wheel can be loaded with names and randomly sorted.
- The wheel will spin according to the strength it starts with.
- It will then slowly stop and pick a random ticket.

The MagicHat Plugin.
- The hat can be filled with names.
- The hat will shake and mix up the list.
- It will then stop and pick a random ticket.
<br>

<h4>Ticket List</h4>
The Ticket Pool holds all tickets.

This list is actually separate from the plugin’s internal list and is coded to update along-side it.

- Click “+” to enter a new ticket.
  - Set a name, color, and ticket count before adding.
- Double click an entry to edit.
  - You can change the color, or Add/Subtract ticket the count.
  - Subtract the ticket count below 1 to remove the entry.
  - Updating will reset the entry to your current setting.

<h4>Advanced</h4>
Advanced option will expand the GUI to show more on-screen options.
<br>
<br>

The Automatic Pic option will allow multiple Winners to be selected automatically.
- All will continuously pick until the Ticket Pool is empty.
- Count will continuously pick until the counter reaches 0.

The pick strength slider allows you to control how long the picking process will take.
- Weak, Average, or Strong will be a short to longer picking time.
- Super will be a long picking time.
- Random will choose a random setting between Weak and Super.
- Infinite will allow the picking process to continue until it is manually stopped.
    - Stopping the pick on Infinite will still choose a random ticket and will not immediately stop.

The Shuffle button will manually mix up the list so that tickets are not clumped together.
- The internal plugin list will first be shuffled.
- The GUI list will be updated accordingly.
- If shuffled, the internal list will be randomized.
<br>
<br>
Reflection training
I wrote this application using Reflection so that the dll plugins can be dropped in without needing to recompile the application. It was also a nice training experience utilizing processes that I normally don’t use.