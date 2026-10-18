# Koffee Kraze

Koffee Kraze is a single-player game that integrates with hardware components.  
<img width="806" height="464" alt="image" src="https://github.com/user-attachments/assets/d8027090-16b1-46f4-8c99-d3c257861a93" />

Created at SwampHacks XII

## Inspiration
There's a lot of talk nowadays about how AI will take people's jobs (specifically entry-level jobs). We wanted to explore a scenario where you compete against AI and prove that you are still a valuable asset in the workforce.
## What it does
Backstory: It is the year 2050, and Artificial Intelligence has taken over the world. You are a part of the last remaining human-run coffee shop on Earth. You must complete coffee orders and return transactions for your customers in a fast and efficient manner, as to not be bought out and taken over by your robot-run competitors StarBots and its brutal AI manager PSL.

Rules: 
Customers will give you their coffee order through an order ticket. At the bottom of the ticket will be the ingredients needed for their order. Your task is to select the correct drink size and ingredients from the menu to serve the customer as quickly as possible. If you need to edit your ingredients, press the undo button or clear button. Once you serve your customer, you will be directed to the cash register. Your customer will give their payment for their drink and you must provide the proper amount in change through the keypad. If mistakes are made during this process, $0.25 will be deducted from your income for the day per mistake. The more accurate your orders are, the less likely that AI will take over your job as indicated by the red bar at the top of the screen. Each day is 90 seconds as shown at the top of the screen, while customer patience times vary depending on the customer (this is the timer shown under the order ticket). Customer satisfaction and accuracy of both orders and change all contribute to the total number of points you earn. As long as you have more points than Mr. PSL you won’t be bought out by StarBots.

## How we built it
We used Unity 6.4 as our game engine in C# with coding support in VSCode IDE. We used both Claude and Gemini as AI assistants to aid with writing scripts and debugging for sake of time and efficiency. Our environment was focused in Unity’s 3D Built-in Renderer pipeline.
All icons within the game are hand-drawn by Nikitha. Main menu screen is attributed to a prefab found in the Unity assets store.  

### Game Loop  
GameManager runs the game as a series of days. Each day lasts 90 seconds and is a series of orders.
An order is generated. OrderManager picks a random unlocked recipe, a customer and a cup size. The time limit comes from that customer’s patience, and it gets a little shorter each day. Drinks and ingredients unlock as the days go on.
The order moves through stations in turn. Each station is a step that finishes before the next begins.
Assembly: you drag ingredients into the cup and choose a size. You can Undo or Clear.
Checkout: you serve the drink and ring up the sale on the register. The register is hardware-ready, with both hardware and on-screen keypad versions behind one interface, so the game doesn’t care which one is connected.
The order is scored. ScoreCalculator combines two things: how accurate the drink was (the share of it made correctly) and how much of the time limit was left. That gives points, and EconomyManager turns it into money and tips. A timed-out order counts as a failure.
The rival plays the same order. For every order, a simulated rival shop gets its own score, based on its current strength. The totals are compared, and the bottom bar shows “You 92 vs Rival 59.”
The takeover meter moves. TakeoverManager raises it when the rival beats you and lowers it when you win. A buyout offer pops up as it fills, and at full the game is over.
The day ends. The game builds a day summary covering money, points, accuracy, speed, how you did against the rival, and which drinks you made. It then saves your progress.

## Challenges we ran into
For most of our members, it was our first time using Unity and doing game development. It took some time to set up the development environments for all our members and get the game running successfully. We had some issues with GitHub's LFS due to the large amount of Unity files. Additionally, it was our first time using ESP32 and WebSockets, so integrating that into Unity in order to interact with the game using hardware took some time.

## Accomplishments that we're proud of
We are very proud that we took a step out of our comfort zone and managed to create a functioning game in such a short time frame. We were also proud to integrate a hardware component into a software application. It was very cool to see how the different components communicated, and it took lots of trial and error to establish the WebSocket connection and send data back and forth across the software and hardware.

## What we learned
We gained more experience with game development and using Unity. We were also able to learn a lot about WebSockets and how hosting a server on an ESP32 works. Additionally, we enhanced our debugging skills as we worked on the hardware integration.

## What's next for Koffee Kraze
- Enhanced hardware integration: joystick for input, LCD screen to display orders, countdown timer, 3D encasing for hardware
- Expanded menu items and recipes
- Deeper Gemini API integration, with different personalities for Mr. PSL
