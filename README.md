# FastFoodDelivery
Fast Food Delivery is a .CS Script for Grand Theft Auto

This script is for a pizza delivery mini-game within the Grand Theft Auto V (GTA V) game environment. 


Here's a breakdown of the key functionalities:

1. **Delivery Points Management:**
   - The script allows the player to save their current position as a delivery point.
   - Delivery points are stored in an INI file.

2. **Pizza Delivery Logic:**
   - Players can initiate pizza delivery by pressing the 'E' key when near the start location (Guidos Takeout on Vinewood).
   - Two delivery modes exist: single delivery or a job with multiple deliveries.
   - Delivery points are randomly generated within a certain distance from the start location.
   - Players are notified about the delivery destination and are provided with a waypoint.
   - Upon reaching the destination, players are rewarded based on distance, potential bonuses, and a base amount.

3. **Faggio Pizza Box:**
   - The script includes a custom class (`FaggioPizzaBox`) that handles the creation, attachment, and deletion of a pizza box prop to a Faggio vehicle. The pizza box visually represents the delivery.

4. **UI Notifications:**
   - The script utilizes UI notifications to inform the player about the status of the pizza delivery, earnings, and other relevant information.

5. **Animation and Props:**
   - An animation is played when a delivery is completed.
   - A pizza box prop is spawned at the delivery point and deleted after a delay.

6. **Script Activation and Deactivation:**
   - The script can be toggled on and off using the 'F9' key.
   - Players can save new delivery points by pressing the 'F8' key.

7. **Settings and Initialization:**
   - Script settings and delivery points are loaded from an INI file during the script initialization.

