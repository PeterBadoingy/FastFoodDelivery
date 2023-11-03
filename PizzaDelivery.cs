using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PizzaDelivery
{
    // Add summary comments to describe the purpose of the class
    public class Main : Script
    {
        // Constants
        private const float InteractionRadius = 2.0f;
        private const float DeliveryRadius = 2.0f;
        private const int MaxEarnings = 25;
        private const int NotificationDuration = 2000;
        private const string PizzaBoxModel = "prop_pizza_box_01";
        private const float MaxDeliveryDistance = 3000.0f;

        // Start location for pizza delivery
        private Vector3 startLocation = new Vector3(443.7377f, 135.1464f, 100.0275f);
        private Random random = new Random();
        private int lastNotificationTime = 0;
        private string currentNotification = "";

        // Flag to track whether the script is active
        private bool isScriptActive = true;

        private FaggioPizzaBox faggioPizzaBox;

        // List to store delivery points
        private List<DeliveryPoint> deliveryLocations = new List<DeliveryPoint>();

        // Flags to track delivery status
        private bool isOnDelivery = false;
        private bool hasCompletedDelivery = false;
        private DeliveryPoint currentDeliveryPoint;

        // Script settings
        private ScriptSettings settings;

        // Flag to track whether the player is at the start location
        private bool isAtStartLocation = false;

        // List to store deliveries for the current job
        private List<DeliveryPoint> currentJobDeliveries = new List<DeliveryPoint>();
        private int currentDeliveryIndex = 0;

        // Constructor
        public Main()
        {
            LoadModules();
            SubscribeToEvents();
            LoadSettingsAndDeliveryPoints();
        }

        // Method to load required modules and settings
        private void LoadModules()
        {
            Function.Call(Hash.REQUEST_ANIM_DICT, "mp_safehouselost@");
            Function.Call(Hash.REQUEST_MODEL, PizzaBoxModel);

            faggioPizzaBox = new FaggioPizzaBox(startLocation);
        }

        // Method to subscribe to events
        private void SubscribeToEvents()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
        }

        // Method to load settings and delivery points from the ini file
        private void LoadSettingsAndDeliveryPoints()
        {
            settings = ScriptSettings.Load("scripts\\PizzaDelivery.ini");
            LoadDeliveryPoints();
        }

        // Method to save the current player position as a delivery point in the .ini file
        private void SaveCurrentPositionAsDeliveryPoint()
        {
            Ped player = Game.Player.Character;
            Vector3 currentPosition = player.Position;

            string deliveryPointName = Game.GetUserInput(30);

            if (!string.IsNullOrWhiteSpace(deliveryPointName))
            {
                DeliveryPoint newDeliveryPoint = new DeliveryPoint(deliveryPointName, currentPosition);
                deliveryLocations.Add(newDeliveryPoint);

                SaveDeliveryPoints();

                currentNotification = "New delivery point '" + deliveryPointName + "' added at current location.";
            }
            else
            {
                UI.Notify("Invalid delivery point name. Please try again.");
            }
        }

        // Load delivery points from the ini file
        private void LoadDeliveryPoints()
        {
            deliveryLocations.Clear();

            int pointCount = settings.GetValue("DeliveryPoints", "Count", 0);

            for (int i = 0; i < pointCount; i++)
            {
                string pointData = settings.GetValue("DeliveryPoints", "Point" + i, string.Empty);

                if (!string.IsNullOrEmpty(pointData))
                {
                    string[] pointValues = pointData.Split(',');

                    if (pointValues.Length == 4)
                    {
                        string pointName = pointValues[0].Trim();
                        float x = float.Parse(pointValues[1].Trim());
                        float y = float.Parse(pointValues[2].Trim());
                        float z = float.Parse(pointValues[3].Trim());

                        Vector3 coordinates = new Vector3(x, y, z);

                        deliveryLocations.Add(new DeliveryPoint(pointName, coordinates));
                    }
                }
            }
        }

        // Save delivery points to the ini file
        private void SaveDeliveryPoints()
        {
            settings.SetValue("DeliveryPoints", "Count", deliveryLocations.Count);

            for (int i = 0; i < deliveryLocations.Count; i++)
            {
                string pointData = deliveryLocations[i].Name + ", " + deliveryLocations[i].Coordinates.X + ", " + deliveryLocations[i].Coordinates.Y + ", " + deliveryLocations[i].Coordinates.Z;
                settings.SetValue("DeliveryPoints", "Point" + i, pointData);
            }

            settings.Save();
        }

        // Event handler for key press
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Check if the F9 key is pressed to enable or disable the script
            if (e.KeyCode == Keys.F9 && e.Modifiers == Keys.None)
            {
                // Toggle the script on or off
                isScriptActive = !isScriptActive;

                // Notify the player about the script status
                if (isScriptActive)
                {
                    UI.Notify("Pizza Delivery enabled. Press F9 to disable.");
                }
                else
                {
                    UI.Notify("Pizza Delivery disabled. Press F9 to enable.");
                }
            }

            // Check if the F8 key is pressed to save and add new delivery positions
            if (e.KeyCode == Keys.F8 && e.Modifiers == Keys.None)
            {
                SaveCurrentPositionAsDeliveryPoint();
            }

            // Check if the script is active
            if (!isScriptActive)
            {
                return;
            }

            // Check if the E key is pressed for pizza delivery and the player is not on a delivery
            if (e.KeyCode == Keys.E && !isOnDelivery)
            {
                Ped player = Game.Player.Character;

                // Check if the E key is pressed for pizza delivery and the player is not on a delivery
                if (e.KeyCode == Keys.E && !isOnDelivery)
                {

                    // Check if the player is at or near the start location
                    if (player.Position.DistanceTo(startLocation) < InteractionRadius)
                    {
                        HandleDeliveryInteraction();

                        CreatePizzaBox(startLocation);

                        // Attach the pizza box to the Faggio
                        faggioPizzaBox.AttachTo(Game.Player.Character);
                    }
                    else
                    {
                        UI.Notify("Get to Guidos Takeout on Vinewood to begin.");
                    }
                }
            }
        }
        // Event handler for script tick
        private void OnTick(object sender, EventArgs e)
        {
            // Check if the script is active
            if (!isScriptActive)
            {
                return;
            }

            int gameTime = Game.GameTime;
            Ped player = Game.Player.Character;

            // Check if the player is at the start location
            isAtStartLocation = player.Position.DistanceTo(startLocation) < InteractionRadius;

            // Check if the player is on a delivery or has completed a delivery
            if (isOnDelivery && !hasCompletedDelivery)
            {
                HandleDeliveryInteraction();
            }
            else if (!isOnDelivery && !hasCompletedDelivery)
            {
                // Check if the player is at the start location
                if (isAtStartLocation)
                {
                    // Notify the player to press E for delivery
                    if (lastNotificationTime == 0 || gameTime - lastNotificationTime > NotificationDuration)
                    {
                        currentNotification = "Press E to deliver some Pizza.";
                        UI.Notify(currentNotification);
                        lastNotificationTime = gameTime;
                    }
                    // Check if the player presses E to start a delivery
                    if (Game.IsKeyPressed(Keys.E) && !isOnDelivery)
                    {
                        HandleDeliveryInteraction();
                    }
                }
                else
                {
                    // Clear the notification if the player is not at the start location
                    ClearNotification();

                    if (faggioPizzaBox != null)
                    {
                        faggioPizzaBox.Delete();
                    }
                }
            }
        }

        // Method to handle pizza delivery interaction
        private void HandleDeliveryInteraction()
        {
            int gameTime = Game.GameTime;

            if (!hasCompletedDelivery)
            {
                // Check if the player is not on a delivery
                if (!isOnDelivery)
                {
                    // Check if there are delivery locations available
                    if (deliveryLocations.Count > 0)
                    {
                        // Decide whether to generate a new job or a single delivery
                        if (random.Next(0, 100) < 50)
                        {
                            // Generate a new job with random delivery points
                            int jobDeliveryCount = random.Next(2, 4);

                            currentJobDeliveries.Clear();
                            HashSet<DeliveryPoint> selectedPoints = new HashSet<DeliveryPoint>();

                            // Select unique delivery points for the job
                            while (currentJobDeliveries.Count < jobDeliveryCount && selectedPoints.Count < deliveryLocations.Count)
                            {
                                DeliveryPoint jobDelivery = deliveryLocations[random.Next(deliveryLocations.Count)];

                                if (selectedPoints.Add(jobDelivery) && startLocation.DistanceTo(jobDelivery.Coordinates) <= MaxDeliveryDistance)
                                {
                                    currentJobDeliveries.Add(jobDelivery);
                                }
                            }

                            selectedPoints.Clear();

                            // Initialize delivery job variables
                            currentDeliveryIndex = 0;
                            currentDeliveryPoint = currentJobDeliveries[currentDeliveryIndex];

                            // Notify the player about the first delivery in the job
                            currentNotification = "Deliver this Pizza to " + currentDeliveryPoint.Name + " (1 of " + jobDeliveryCount + ")";
                            UI.Notify(currentNotification);

                            // Set a waypoint to the current delivery point
                            Function.Call(Hash.SET_NEW_WAYPOINT, currentDeliveryPoint.Coordinates.X, currentDeliveryPoint.Coordinates.Y);

                            // Set the flag to indicate the player is on a delivery
                            isOnDelivery = true;
                            lastNotificationTime = gameTime;

                            // Create the pizza box prop when starting the delivery
                            CreatePizzaBox(startLocation);
                        }
                        else
                        {
                            // Generate a single delivery with a random eligible point
                            List<DeliveryPoint> eligiblePoints = deliveryLocations
                                .Where(point => startLocation.DistanceTo(point.Coordinates) <= MaxDeliveryDistance)
                                .ToList();

                            if (eligiblePoints.Count > 0)
                            {
                                // Select a random eligible point for a single delivery
                                currentDeliveryPoint = eligiblePoints[random.Next(eligiblePoints.Count)];
                                currentNotification = "Take this Pizza to " + currentDeliveryPoint.Name;
                                UI.Notify(currentNotification);

                                // Set a waypoint to the current delivery point
                                Function.Call(Hash.SET_NEW_WAYPOINT, currentDeliveryPoint.Coordinates.X, currentDeliveryPoint.Coordinates.Y);

                                // Set the flag to indicate the player is on a delivery
                                isOnDelivery = true;
                                lastNotificationTime = gameTime;
                            }
                            else
                            {
                                // Notify the player if no eligible delivery points are available
                                UI.Notify("No eligible delivery points available within the allowed distance.");
                            }
                        }
                    }
                    else
                    {
                        // Notify the player if no delivery points are available
                        UI.Notify("No delivery points available.");
                    }
                }
                else if (currentDeliveryPoint != null && Game.Player.Character.Position.DistanceTo(currentDeliveryPoint.Coordinates) < DeliveryRadius)
                {
                    // Check if the player is at the delivery point for completing the delivery
                    if (lastNotificationTime == 0 || gameTime - lastNotificationTime > NotificationDuration)
                    {
                        FinishDelivery(currentDeliveryPoint);
                        lastNotificationTime = gameTime;
                    }
                }
                else
                {
                    // Check if the player presses E to view the delivery address
                    if (Game.IsKeyPressed(Keys.E) && currentDeliveryPoint != null)
                    {
                        UI.Notify("The Address is: " + currentDeliveryPoint.Name);

                        // Set a new waypoint to the current delivery point
                        Function.Call(Hash.SET_NEW_WAYPOINT, currentDeliveryPoint.Coordinates.X, currentDeliveryPoint.Coordinates.Y);
                    }
                }
            }
        }

        // Method to finish a delivery and calculate earnings
        private void FinishDelivery(DeliveryPoint deliveryPoint)
        {
            int gameTime = Game.GameTime;

            // Check if the player is close to the delivery point
            if (Game.Player.Character.Position.DistanceTo(deliveryPoint.Coordinates) < DeliveryRadius)
            {
                // Calculate earnings based on distance, bonus, and base amount
                int baseEarnings = MaxEarnings;

                float distance = World.GetDistance(Game.Player.Character.Position, deliveryPoint.Coordinates);

                float distanceFromStart = World.GetDistance(startLocation, deliveryPoint.Coordinates);
                int distanceEarnings = (int)Math.Round(distanceFromStart * 0.1);

                bool hasBonus = random.Next(0, 100) < 50;

                int bonusAmount = 0;
                if (hasBonus)
                {
                    int minBonusPercentage = 20;
                    int maxBonusPercentage = 75;
                    int bonusPercentage = random.Next(minBonusPercentage, maxBonusPercentage + 1);
                    bonusAmount = (int)Math.Round((baseEarnings * bonusPercentage) / 100.0);
                }

                int totalEarnings = baseEarnings + distanceEarnings + bonusAmount;

                // Add earnings to the player's money
                Game.Player.Money += totalEarnings;

                // Notify the player about earnings and details of the delivery
                if (hasBonus)
                {
                    currentNotification = "Delivery to " + deliveryPoint.Name + " done! You got: $" + baseEarnings + " + Distance Tip: $" + distanceEarnings + " + Customer Tip: $" + bonusAmount + " Total: $" + totalEarnings;
                }
                else
                {
                    currentNotification = "Delivery to " + deliveryPoint.Name + " done! You got: $" + baseEarnings + " + Distance Tip: $" + distanceEarnings + " Total: $" + totalEarnings;
                }

                UI.Notify(currentNotification);

                // Play a delivery animation
                string animationDictionary = "mp_safehouselost@";
                string animationName = "package_dropoff";
                float blendInSpeed = 8f;
                float blendOutSpeed = -8f;
                int duration = -1;

                Function.Call(Hash.REQUEST_ANIM_DICT, animationDictionary);

                while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animationDictionary))
                {
                    Wait(100);
                }

                Game.Player.Character.Task.PlayAnimation(animationDictionary, animationName, blendInSpeed, -1, AnimationFlags.UpperBodyOnly | (AnimationFlags)8);

                Wait(3000);

                Game.Player.Character.Task.ClearAnimation(animationDictionary, animationName);
                Function.Call(Hash.REMOVE_ANIM_DICT, animationDictionary);

                // Spawn a pizza box prop at the delivery point
                float lowerYOffset = -1.0f;
                Vector3 pizzaBoxPosition = new Vector3(deliveryPoint.Coordinates.X, deliveryPoint.Coordinates.Y, deliveryPoint.Coordinates.Z + lowerYOffset);
                Prop pizzaBox = World.CreateProp(PizzaBoxModel, pizzaBoxPosition, true, false);
                pizzaBox.FreezePosition = true;

                Wait(3000);

                // Delete the pizza box prop after a delay
                if (pizzaBox.Exists())
                {
                    pizzaBox.Delete();
                }

                // Move to the next delivery in the current job
                currentDeliveryIndex++;

                if (currentDeliveryIndex < currentJobDeliveries.Count)
                {
                    currentDeliveryPoint = currentJobDeliveries[currentDeliveryIndex];
                    Function.Call(Hash.SET_NEW_WAYPOINT, currentDeliveryPoint.Coordinates.X, currentDeliveryPoint.Coordinates.Y);

                    currentNotification = "Next delivery: " + currentDeliveryPoint.Name + " (" + (currentDeliveryIndex + 1) + " of " + currentJobDeliveries.Count + ")";
                    UI.Notify(currentNotification);
                }
                else
                {
                    // Notify the player when all deliveries in the job are complete
                    currentNotification = "All deliveries complete! Return to the shop for more work.";
                    UI.Notify(currentNotification);

                    // Delete the pizza box prop when all deliveries are complete
                    DeletePizzaBox();


                    // Reset delivery state flags and clear job deliveries list
                    isOnDelivery = false;
                    hasCompletedDelivery = true;
                    lastNotificationTime = gameTime;

                    ResetDeliveryState();
                    currentJobDeliveries.Clear();
                }
            }
            else
            {
                // Notify the player to get closer to the delivery point
                currentNotification = "Get closer to the delivery point to complete the delivery.";
                //UI.Notify(currentNotification);
            }
        }

        // Method to reset delivery state flags
        private void ResetDeliveryState()
        {
            hasCompletedDelivery = false;
            currentDeliveryPoint = null;
            currentDeliveryIndex = 0;
        }

        // Method to clear the current notification
        private void ClearNotification()
        {
            if (!string.IsNullOrEmpty(currentNotification))
            {
                UI.Notify(currentNotification);
                currentNotification = "";
            }
        }

        private void CreatePizzaBox(Vector3 position)
        {
            // Delete existing pizza box if it exists
            DeletePizzaBox();


            // Create a pizza box at the specified position
            faggioPizzaBox = new FaggioPizzaBox(position);
        }

        public void DeletePizzaBox()
        {
            // Delete the pizza box
            faggioPizzaBox.Delete();
        }

        public class FaggioPizzaBox
        {
            private Prop pizzaBox;

            public FaggioPizzaBox(Vector3 position)
            {
                pizzaBox = World.CreateProp(PizzaBoxModel, position, true, false);
                pizzaBox.FreezePosition = true;
                // Debug print or notify
                //UI.Notify("Pizza box created at " + position);
            }

            public void AttachTo(Ped ped)
            {
                // Check if the player is using a Faggio and attach the prop to it
                if (Game.Player.Character.IsInVehicle() && Function.Call<int>(Hash.GET_ENTITY_MODEL, Game.Player.Character.CurrentVehicle.Handle) == unchecked((int)VehicleHash.Faggio))
                {
                    // Set the attachment position and rotation
                    Vector3 attachmentPosition = new Vector3(-0.00499999f, -0.8f, 0.315f);
                    Vector3 attachmentRotation = new Vector3(0f, 0f, -0.650002f);

                    // Attach the prop to the Faggio
                    pizzaBox.AttachTo(Game.Player.Character.CurrentVehicle, Game.Player.Character.CurrentVehicle.GetBoneIndex("baggage"), attachmentPosition, attachmentRotation);
                    // Debug print or notify
                    //UI.Notify("Pizza box attached to Faggio");
                }
                else
                {
                    // Delete the pizza box if the player is not in a Faggio
                    pizzaBox.Delete();
                    // Debug print or notify
                    //UI.Notify("Pizza box deleted. Player is not in a Faggio.");
                }
            }

            public void Delete()
            {
                if (pizzaBox.Exists())
                {
                    pizzaBox.Delete();
                    // Debug print or notify
                    //UI.Notify("Pizza box deleted");
                }
            }
        }

        // Class to represent a delivery point with a name and coordinates
        private class DeliveryPoint
        {
            private string _name;
            private Vector3 _coordinates;

            public string Name
            {
                get { return _name; }
            }

            public Vector3 Coordinates
            {
                get { return _coordinates; }
            }

            public DeliveryPoint(string name, Vector3 coordinates)
            {
                _name = name;
                _coordinates = coordinates;
            }
        }
    }
}
