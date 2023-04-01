#ifndef GAME_H
#define GAME_H

#include <stdint.h>
#include "DAC.h"
#include "Display.h"
#include "random.h"
#include "Inputs.h"

#define MUSIC_BUFFER_SIZE 16
#define FX_BUFFER_SIZE 16

//Main game balancing controls
#define gameMovementSpeed 1 //Based on ticks? DO NOT CHANGE
#define gameFPS 10 //could be uncapped?
#define gameTickRate 20 //controls attackRates and animations
#define damageRatio 1
#define healthRatio 1
#define animationRatio 1//general animation speed ratio

#define speedMultiplier gameMovementSpeed
#define peaSpeed 3*speedMultiplier
#define icePeaSpeed speedMultiplier
#define sunSpeed 1*speedMultiplier
#define zombieSpeed 1*speedMultiplier
#define poleVaultSpeed zombieSpeed*4
#define jackZombieSpeed zombieSpeed*3
#define footballSpeed poleVaultSpeed
#define newspaperAngrySpeed jackZombieSpeed
#define lawnmowerSpeed 3

#define peaDamage 1*damageRatio
#define ohkoDamage 50*damageRatio
#define chompDamage ohkoDamage
#define explosionDamage ohkoDamage
#define smallExplosionDamage ohkoDamage
#define jackInTheBoxDamage ohkoDamage
#define zombieDamage 1*damageRatio
#define lawnmowerDamage 50*damageRatio

#define defaultPlantHealth 8*healthRatio
#define wallNutHealth 100*healthRatio
#define defaultZombieHealth 10*healthRatio
#define coneHealth 10*healthRatio
#define bucketHealth 20*healthRatio
#define newspaperHealth 8*healthRatio
#define helmetHealth 30*healthRatio
//TO-DO

#define	PlantAttackRate 1.5*gameTickRate
#define repeaterRepeatTicks 0.2*gameTickRate
#define ZombieAttackRate 0.4*gameTickRate
#define sunProductionRate 10*gameTickRate
#define potatoMineSurfaceTime 255
#define chomperChewTime 255

#define defaultAnimationTime gameTickRate*animationRatio
#define DefaultPlantAnimationRate 0.5*defaultAnimationTime
#define CherryBombAnimationRate 0.2*defaultAnimationTime
#define ExplosionTime 0.2*defaultAnimationTime
#define ChompAnimationRate 0.2*defaultAnimationTime
#define DefaultZombieAnimationRate 0.3*defaultAnimationTime
#define FootballAnimationRate 0.1*defaultAnimationTime
#define PoleVaultAnimationRate 0.1*defaultAnimationTime
#define EatingZombieAnimationRate 0.1*defaultAnimationTime



#define cmpButtonXSize 50
#define cmpButtonYSize 30
#define	vsButtonXSize 50
#define vsButtonYSize 30
#define langButtonXSize 50
#define langButtonYSize 30

#define CmpButtonXpos 10
#define CmpButtonYpos 50
#define VsButtonXpos 90
#define VsButtonYpos 50
#define LangButtonXpos 80
#define LangButtonYpos 10

#define SpXSize 10 //size of all seed packet sprites
#define SpYSize 8 
#define SpOffset SpXSize+2
#define SpYpos 118	//y position of all seed packets
#define FspXpos 18		//x position of left most packet
#define SspXpos FspXpos + SpOffset * 1
#define TspXpos FspXpos + (SpOffset * 2)
#define	FospXpos FspXpos + (SpOffset * 3)
#define FispXpos FspXpos + SpOffset * 4
#define SispXpos FspXpos + SpOffset * 5
#define SespXpos FspXpos + SpOffset * 6
#define EspXpos FspXpos + SpOffset * 7

#define peashooterCost 100
#define sunflowerCost 50
#define snowPeaCost 175
#define	repeaterCost 200
#define chomperCost 150
#define potatoMineCost 25
#define cherryBombCost 150
#define wallNutCost 50


#define LaneYOffset gridY //change later
#define Lane1Ypos ZeroY
#define Lane2Ypos Lane1Ypos + gridY * 1
#define Lane3Ypos Lane1Ypos + gridY * 2
#define Lane4Ypos Lane1Ypos + gridY * 3
#define Lane5Ypos Lane1Ypos + gridY * 4

#define ZombieStartXpos
#define collectTolerance 30
//change later:
#define transparentColor 0xFB56
#define ZeroX 14
#define ZeroY	5
#define gridX 16
#define gridY 20
#define shootOffsetX 15
#define shootOffsetY 8
	//packet load times;
#define LoadTime 100
#define bigWaveSize 4 //number of zombies in a big wave
#define stickLeftTolerance 3800
#define stickRightTolerance 500
#define stickUpTolerance 3800
#define stickDownTolerance 500

#define sceneInputRate 10


// Sprite contains a pointer to a bitmap, and has a length and width in pixels.
class SpriteType{
	public:
		const uint16_t* bmp;	// Bitmap of half words to encode RGB info
		uint8_t length;	// Length
		uint8_t width;	// Width
		SpriteType* nextSprite;	// Sprite pointer for animation FSM (each sprite only has one next sprite)
	public:
		SpriteType(const uint16_t* bmp, uint8_t length, uint8_t width, SpriteType* nextSprite);
};



// Any object on the screen that needs to be rendered/unrendered and does something
class GameObject{
	public:
		SpriteType* previousSprite; //when we advance, set the previousSprite to the sprite if we change sprites
		SpriteType* sprite;	// Sprite pointer
		uint8_t redraw; //1 or 0, initialize to 1, only render if 0
		Sound* soundFX;	// Sound effect
		uint8_t x;	// X position
		uint8_t y;	// Y position
		uint8_t lane;
		uint8_t oldx;
		uint8_t oldy;
	
		// Clear the current pixels of the game object, replacing them with the background
		void unrender();
		
		// Advance to the next state of the game object (should be overloaded)
		virtual void advance();
	
		// Render the current state of the game object
		void render();
	
		// unrender pixels and replace them one at a time
		void rerender();
	
		
	public:
		// Constructor
		//GameObject();
	
		// Constructor with parameters
		GameObject(SpriteType* sp, Sound* sfx, uint8_t x, uint8_t y);
		GameObject(SpriteType* sp, Sound* sfx, uint8_t x, uint8_t y, uint8_t lane);
	
		
		// Destructor
		//~GameObject();
	
		// Copy Constructor
		//GameObject(const GameObject& other);
	
		// Assignment Operator
		//GameObject operator=(GameObject& other);
		
		// Unrender, advance to next state of the game object and render
		virtual void refresh();
		//game object tick will not do anything
		virtual void tick();
		//game object collide will not do anything
		int collided();
		uint8_t getX();
		uint8_t getY();
		uint8_t getLane();
};

// GameObject with health (plants and zombies)
class Entity: public GameObject{
	public:
		int16_t health;	// Health
		uint8_t animationTime;	// time to switch animation sprites
		uint8_t animationTimer;
		uint8_t hostile; //0 or 1
		
		// Advance to the next state of the entity and call attack?
		void advance();
		// do attacking sequence if hostile
		
	public:
		//constructor with all new parameters, calls parent constructor for first 4
		Entity(SpriteType* sp, Sound* sfx, uint8_t xpos, uint8_t ypos, uint8_t hp, uint8_t anim, uint8_t hostl, uint8_t lane);
		//entity tick will decrement animationTime
		void tick();
		virtual void hurt(uint8_t damage);
};

// Projectile
class Projectile: public GameObject{
	protected:
		
		uint8_t speed;	// Speed
		uint8_t distanceDiff; //increments by speed every tick, advance adds it to xpos and sets redraw to 1;
		uint8_t collision; //1 or 0
		void advance();
	public:
		uint8_t damage;	// Damage
		//constructor with all new parameters, calls parent constructor for first 4
		Projectile(SpriteType* sp, Sound* sfx, uint8_t xpos, uint8_t ypos, uint8_t spd, uint8_t dam, uint8_t lane);
		virtual int collided();
		//projectile tick will change their xpos by their speed and set redraw to 1, if speed > 0
		void tick();
};

class LawnMower: public Projectile{
	protected:
		uint8_t isMoving; //1 or 0
		//change advance so it only moves if isMoving == 1
		void advance();
		void reset();
	public:
		//constructer calls Projectile with lawnmower sprite, lawnmower sfx, x and y arguments, lawnmower speed, lawnmower damage
		//set isMoving to 0
		LawnMower(uint8_t x, uint8_t y, uint8_t lane);
		//change collided so it doesn't kill lawnmower, sets isMoving to 1 and deals damage to zombies
		int collided();
		//lawnmower tick will change xpos by speed and set redraw to 1 if isMoving == 1
		void tick();
	};	

class Pea : public Projectile{
	public:
		//set projectile variables to defined pea damage, speed, sprite, sfx, collision = 1
		Pea(uint8_t x, uint8_t y, uint8_t lane);
};

class FrozenPea : public Projectile{
	public:
		//set projectile variables to defined Snowpea damage, sprite, pea sfx, and pea speed, collision = 1
		FrozenPea(uint8_t x, uint8_t y, uint8_t lane);
};

// One hit KO (e.g. Cherry Bomb, Potato Mine, Chomper)
class Ohko: public Projectile{
	protected:
		uint8_t explosionTimer;
		void advance();
	public:
		//set projectile variable to defined OHKO damage, OHKO (transparent) sprite, sfx, speed = 0, collision = 1
		Ohko(uint8_t x, uint8_t y, uint8_t lane);
		//Special constructor does same as other constructor, but sets sprite to argument
		Ohko(uint8_t x, uint8_t y, uint8_t lane, SpriteType* sprite, Sound* sound);	
		
		void tick();
};

class Explosion : public Ohko{
	protected:	
		//change advance so the projectile goes away after explosionTimer 
		void render();
		void unrender();
	public:
		//call Ohko constructor in the 8 surrounding squares as well as this one. In this square, use big explosion sprite.
		//initialize
	  Explosion(uint8_t x, uint8_t y, uint8_t lane);
		//change collided so the projectile does not go away when collision happens
		int collided();
		
};

class SmallExplosion : public Ohko{
	public:
		//call Ohko constructor in this square, use small explosion sprite
		SmallExplosion(uint8_t x, uint8_t y, uint8_t lane);
		//change collided so the projectile does not go away when collision happens
		int collided();
};

class Chomp : public Ohko{
	//I don't know if it needs to be different than Ohko or not
	public:
		Chomp(uint8_t x, uint8_t y, uint8_t lane);
};

class Sun : public Projectile{
	protected:
  uint8_t upTimer;
	int16_t distance;
	uint8_t isMoving;
		//change advance so if collision with cursor, collect sun
		void advance();
	public:
		//call projectile constructor, use sun sprite, sun sfx, speed as defined sun speed, collision = 0, damage = 0
		Sun(uint8_t x, uint8_t y, uint8_t isMoving);
		//change tick so it changes y pos, not x pos
		void tick();
};



class Plant : public Entity{
	protected:
		uint8_t attackRate;
		uint8_t attackTimer;
		uint8_t projID;
		uint8_t col;
		//create projectile
		virtual void attack();
		//idk why I put this here
		void advance();
	public:
		//call entity constructor with all but attackRate and projectile
		Plant(SpriteType* sp, uint8_t xpos, uint8_t ypos, uint8_t hp, 
					uint8_t hostile, uint8_t atkRt, uint8_t projID, uint8_t lane);
		
		void hurt(uint8_t damage);
		void tick();
		void setCol(uint8_t col);
		uint8_t getCol();
		
};

class Peashooter : public Plant{
		
	protected: 
	
	public:
		//constructor calls Plant constructor with defined peashooter sprite, 
		//defined peashooter sound, x and y arguments, defined generic plant health,
		//generic plant animation time, hostile = 1, atkRt as generic plant attack rate
		//and pea projectile
		Peashooter(uint8_t x, uint8_t y, uint8_t lane);
		//special constructor calls Plant constructor with Sprite argument rather than defined peashooter
		Peashooter(uint8_t x, uint8_t y, uint8_t lane, SpriteType* sp);
		//special constructer calls Plant constructor with Sprite argument and pea argument
		Peashooter(uint8_t x, uint8_t y, uint8_t lane, SpriteType* sp, uint8_t projID);
};
		
class Repeater : public Peashooter{
	protected:
		//time in between 1st and 2nd pea
		uint8_t repeatTimer;
		void advance();
	public:
		//constructor calls peashooter constructor with x and y argument and defined repeater sprite, and sets repeatTime to defined repeater time
		Repeater(uint8_t x, uint8_t y, uint8_t lane);
		void tick();
};
class Snowpea : public Peashooter{
	public:
		//constructor calls peashooter constructor with x and y arguements, Snowpea sprite and snow pea projectile
		Snowpea(uint8_t x, uint8_t y, uint8_t lane);
};

class Wallnut : public Plant{
	protected:
		SpriteType* damagedWallnut;
		//new advance function will check health, and change sprite if it reaches defined threshold
		void advance();


	public:
		//constructor calls Plant constructor with defined Walnut sprite, 
		//null sount, x and y arguments, defined walnut health,
		//generic plant animation time (still need to render when it gets hurt), hostile = 0, atkRt as generic plant attack rate (doesn't matter)
		//and null projectile. Set damagedWallnut to the defined damagedWallnut sprite
		Wallnut(uint8_t x, uint8_t y, uint8_t lane);
		//change to do nothing
		void tick();
};

class CherryBomb : public Plant{
	protected:
		//after attacking, destroy the cherry bomb. also, no range detection
		void attack();
	public:
			//constructor calls Plant constructor with defined cherry bomb sprite, explosion sound, x and y arguments, 
		  //defined generic plant health, generic plant animation time, hostile = 1, atkRt as generic plant attack rate maybe, 
			//explosion projectile
			CherryBomb(uint8_t x, uint8_t y, uint8_t lane);
};

class PotatoMine : public Plant{
	protected:
		//SpriteType* growing; //this might not be implemented
		SpriteType* aboveGround;
		//can only attack if it's grown
		uint8_t grown; //1 or 0
		//after attacking, destroy the PotatoMine. cannot attack if underground (or transitioning). also, different range detection
		void attack();
	
	public:
			//constructor calls Plant constructor with defined potato mine sprite, explosion sound, x and y arguments,
			//defined generic plant health, generic plant animation time, hostile = 1, atkRt as more instantaneous
			//small explosion projectile
			//set growing and aboveGround to sprites
			//set grown to 0
			PotatoMine(uint8_t x, uint8_t y, uint8_t lane);
		void hurt(uint8_t dam);
};
class Sunflower : public Plant{
		//to me, it makes sense for the sun to be a projectile, maybe just have sun incorporate different behavior
	protected:
	public:
		//constructor calls Plant constructor with defined sunflower sprite, null or sun sound, x and y arguments
		//defined generic plant health, generic plant animation time, hostile = 1, atkRt as more slow
		//sun projectile (we can change it from a projectile if we want and just change up the attack function)
		Sunflower(uint8_t x, uint8_t y, uint8_t lane);
			
};

class Chomper : public Plant{
	protected:	
		
		SpriteType* empty;	//for when no zombie in mouth
		SpriteType* bite;	//while attacking, bite sprite should point to chewing sprites
		uint8_t mouthFull; //1 or 0
		//change range detection to one tile
		void advance();
		//if no invisible hitbox projectile, we need to make attack do the collision
		void attack();
		
	public:
			//constructor calls Plant constructor with defined chomper empty sprite, defined chomp sound, x and y arguments
			//defined generic plant health, generic plant animation time, hostile = 1, atkRt as faster than standard
			//null projectile or invisible hitbox?
			//set full sprite to defined full chomper sprite, and empty to empty sprite
			//mouthFull = 0
		Chomper(uint8_t x, uint8_t y, uint8_t lane);
		void hurt(uint8_t dam);
};


class Button : public GameObject{
	protected:
		//does nothing in Button
		virtual void buttonFunction();
	public:
		//calls gameobject with parameters as passed in
		Button(uint8_t x, uint8_t y, SpriteType* sprite, Sound* sfx);
		//plays sound, calls buttonFunction
		virtual void buttonHit();

};

class MenuButton : public Button{
	protected:
		SpriteType* english;
		SpriteType* espanol;
		void advance();
	public:
		//calls Button constructor with parameters as passed in, but set to menu button sound
		MenuButton(uint8_t x, uint8_t y, SpriteType* eng, SpriteType* esp);
		//change advance so it checks what the global language (main.cpp) and changes the current sprite and set redraw to 1
		
};	

class VsButton : public MenuButton{
	protected:
		//change buttonFunction to load VS scene
		void buttonFunction();
	public:
		//calls MenuButton constructor with parameters as passed in, but set to VS button sprite
		VsButton(uint8_t x, uint8_t y);
		
};

class CampaignButton : public MenuButton{
	protected:
		//change buttonFunction to load Campaign scene
		void buttonFunction();
	public:
		//calls MenuButton constructor with parameters as passed in, but set to Campaign button sprites
		CampaignButton(uint8_t x, uint8_t y);
		
};

class LanguageButton : public MenuButton{
	protected:
		//change buttonFunction will toggle global variable language from 0 to 1 or 1 to 0
		void buttonFunction();
	public:
		//calls MenuButton constructor with parameters as passed in, but set to language button sprites
	  LanguageButton(uint8_t x, uint8_t y);
		
};

class SeedPacket : public Button{
	protected:
    uint8_t isReady; // Tracks if ready to be planted
		SpriteType* ready;
		SpriteType* gray;
		uint8_t loadTime;
		uint8_t loadTimer;
		int16_t sunCost;
		uint8_t plantID; //0 for peashooter, 1 for repeater, 2 for snowpea, 3 for wallnut, 4 for cherry bomb, 5 for mine, 6 for chomper, 7 for sunflower
		//change buttonFunction to call global spawn plant with plantID
		void buttonFunction();
		//change advance to check if seed is ready
		void advance();
	public:
		//calls Button constructor with parameters, but use planting sound,  and defined seed packet sprite/plantname from scene model
		SeedPacket(uint8_t x, uint8_t y, uint8_t plantID, uint8_t loadTime, uint8_t sunCost);	
		void buttonHit();
		//tick decrements the load timer
		void tick();
};

class Zombie: public Entity{
	protected:
		SpriteType* walkFSM;	// Walk animation pointer
		SpriteType* eatFSM; // Eat animation pointer
		uint8_t speed;  // Speed of zombie
		uint8_t isEating;   // Is the zombie eating?
		uint8_t wasEating; //was the zombie eating last frame?
		uint8_t damageTimer;
		uint8_t damageTime;
		uint8_t distanceDiff;
		
		// Advance to the next state of the entity
		void advance();
		
		
	public:
		// Constructor
		Zombie(uint8_t xpos, uint8_t ypos, uint8_t lane);

		// Constructor
		Zombie(SpriteType* sp, uint8_t xpos, uint8_t ypos, uint8_t hp, 
			uint8_t anim, uint8_t speed, uint8_t lane);
		Zombie(SpriteType* sp, uint8_t xpos, uint8_t ypos, uint8_t hp, 
			 uint8_t speed, uint8_t lane);
		void tick();
		// do attacking sequence if hostile
		virtual void attack(Plant* plt);
		void stopEating();
		void takeDamage(uint8_t dam);
};

// Regular zombie with a flag. Has random zombies in a wave formation following.
// Scene should have a generate function that generates wave
class FlagZombie: public Zombie{
	private:
		uint8_t spawnDelay;
		uint8_t spawnTimer;
		uint8_t numSpawn;
		void advance();
	public:
		FlagZombie(uint8_t x, uint8_t y, uint8_t lane);
		void tick();
};

// Any zombie with extra health and headwear
class ArmorZombie: public Zombie{
	protected:
		SpriteType* fullWalkFSM;    // Headwear on, undamaged
		SpriteType* fullEatFSM;
		// Redefine advance to change sprite at certain health
		void advance();
	public:
		// Constructor
		ArmorZombie(uint8_t x, uint8_t y, uint8_t lane, SpriteType* fullWalk, SpriteType* fullEat);
};

// Conehead zombie. Only thing different is that it has a different sprite and different health.
class ConeZombie: public ArmorZombie{
	public:
		// Constructor
		ConeZombie(uint8_t x, uint8_t y, uint8_t lane);
};

// Buckethead zombie. Only thing different is that it has a different sprite and different health.
class BucketZombie: public ArmorZombie{
	public:
		// Constructor
		BucketZombie(uint8_t x, uint8_t y, uint8_t lane);
};

// Football zombie. Only thing different is that it has a different sprite, different health, and different speed.
class FootballZombie: public ArmorZombie{
	public:
		// Constructor
		FootballZombie(uint8_t x, uint8_t y, uint8_t lane);
};

// Newspaper zombie. Only thing different is that it has a different sprite, different health, and conditional speed.
class NewsZombie: public ArmorZombie{
	private:
		void advance();
	public:
		// Constructor
		NewsZombie(uint8_t x, uint8_t y, uint8_t lane);
};

// Jack in the box zombie. Blows up after certain amount of time.
class JackZombie: public Zombie{
	private:
	public:
		// Constructor
		JackZombie(uint8_t x, uint8_t y, uint8_t lane);
		void attack(Plant* plt);
};

// Polevault zombie. Jumps over plants.
class PoleZombie: public Zombie{
	private:
		uint8_t hasPole;	// Does the zombie  have its pole?
		uint8_t hadPole; //did the zombie have the pole on the last frame
		SpriteType* jumpSprite; //points to walking sprites

		// Redefine attack to jump over first plant
		void attack(Plant* plt);
	public:
		// Constructor
		PoleZombie(uint8_t x, uint8_t y, uint8_t lane);
};



//Peashooter
//Repeater
//Snow pea
//Wall-nut
//Potato Mine
//Cherry Bomb
//Chomper
//Sunflower

class GameObjectList{
	public:
		GameObject* objects[256];
		uint8_t indexPtr;
	public:
		//Constructor
		GameObjectList(GameObject** GOlist);
		GameObjectList();
		//Destructor
		~GameObjectList();
		//Add Object
		void GOAdd(GameObject* add);
		//Access Object at index
		GameObject* operator[](uint8_t i);
		//Remove Object at index
		void GORmv(uint8_t i);
		void tryRmv(GameObject* go);
		void tryRmv(uint8_t col, uint8_t row);
		void refresh();
		void redrawSet();
		uint8_t getLength();
		//will tick every existing member of objects
		void tick();
};

//Still need to do globals and definitions for this crap

class SelectCursor{
	private:
		uint8_t buttonIndex;
		GameObject* button;
		GameObject* oldButton;
		uint8_t redraw;
		uint8_t updated;
		GameObjectList* targetButtons;
		void render();
		void updatePos();
	public:
		SelectCursor(GameObjectList* gos);
		void refresh();
};

class GridCursor{
	public:
		uint8_t calcX();
		uint8_t calcY();
		uint8_t calcOldX();
		uint8_t calcOldY();
	public:
		uint8_t gridXpos;	//for spawning plants
		uint8_t gridYpos;	//unused for select
		uint8_t oldGridX;
		uint8_t oldGridY;
		uint8_t grid[9][5];
		uint8_t redraw;
		void render();
		void updatePos();
	public:
		GridCursor();
		void refresh();
		uint8_t gridOpen(); //returns 1 if no plant, returns 0 if plant
		void fillGrid();
		void clearGrid();
		void emptyGrid(uint8_t col, uint8_t row);
		
};
// Collection of all game objects, background, music, etc. pertinent to the current area of the game
class Scene{
	private:
		
		uint8_t sunRate;
		uint8_t sunTimer;
		uint8_t inputRate;
		uint8_t inputTimer;
		
		uint8_t hasGrid;
		int32_t zombieTimer;
		
		SelectCursor* select; //menuing and seed packets
	public:
		int16_t sunAmount;
		const uint16_t* backgroundBMP;	// Background of the git as a bitmap
		GameObjectList* Zombies;	// List of all objects on the scene these are arrays of pointers
		GameObjectList* Plants;
		GameObjectList* Buttons;
		GameObjectList* Lawnmowers;
		GameObjectList* Projectiles;
		GridCursor* planter; //locked to the grid hopefully
		// Constructor
		Scene(GameObjectList* but, GameObjectList* lwm, const uint16_t* bg, uint8_t hasGrid);
		// Destructor
		//~Scene();
	
		// Copy Constructor
		//Scene(const Scene& other);
	
		// Assignment Operator
		//Scene operator=(Scene& other);
		const uint16_t* retBG();
	  void refresh();
		void collisions();
		void tick();
		void spawnProjectile(uint8_t projID, uint8_t x, uint8_t y, uint8_t lane);
		void spawnPlant(uint8_t plantID);
		void spawnZombie(uint8_t zombieID, uint8_t lane);
		//return 1 if sun can change
		uint8_t changeSun(int16_t amount);
		void renderSun();
		int cursorHit(int16_t x, int16_t y);
		uint8_t gridCheck();
		//called by jack zombie
		void wipe();
};

//bitmaps
extern const uint16_t menuBackground[20480];
extern const uint16_t lawnBackground[20480];
	
enum zombieIDS{
	regularZombieID, coneZombieID, bucketZombieID, footballZombieID, 
	newspaperZombieID, poleVaultZombieID, jackZombieID, flagZombieID
};

enum projIDS{
	peaID, frozenPeaID, chompID, ohkoID, 
smallExplosionID, explosionID, sunID};

enum plantIDS{
	peashooterID, sunflowerID, snowPeaID, repeaterID, chomperID, potatoMineID, cherryBombID, wallNutID
};

//plant sprites
extern SpriteType* peashooterSprite;
extern SpriteType* peashooterSprite2;
extern SpriteType* peashooterSprite3;
extern SpriteType* peashooterSprite4;

extern SpriteType* snowPeaSprite;
extern SpriteType* snowPeaSprite2;
extern SpriteType* snowPeaSprite3;
extern SpriteType* snowPeaSprite4;

extern SpriteType* repeaterSprite;
extern SpriteType* repeaterSprite2;

extern SpriteType* sunflowerSprite;
extern SpriteType* sunflowerSprite2;

extern SpriteType* cherryBombSprite;
extern SpriteType* cherryBombSprite2;
extern SpriteType* cherryBombSprite3;

extern SpriteType* potatoMineSprite;
extern SpriteType* potatoMineReadySprite;
extern SpriteType* potatoMineReadySprite2;

extern SpriteType* chomperSprite;
extern SpriteType* chomperSprite2;
extern SpriteType* chomperSprite3;
extern SpriteType* chomperSprite4;
extern SpriteType* chomperChewSprite;
extern SpriteType* chomperAttackSprite;

extern SpriteType* wallNutSprite;
extern SpriteType* wallNutDamagedSprite;

//projectile sprites
extern SpriteType* frozenPeaSprite;
extern SpriteType* peaSprite;
extern SpriteType* smallExplosionSprite;
extern SpriteType* largeExplosionSprite;
extern SpriteType* sunSprite;
extern SpriteType* transparentSprite;

//Zombie sprites
extern SpriteType* regularZombieSprite;
extern SpriteType* regularZombieSprite2;
extern SpriteType* regularZombieSprite3;
extern SpriteType* regularZombieSprite4;

extern SpriteType* regularZombieEatSprite;
extern SpriteType* regularZombieEatSprite2;
extern SpriteType* regularZombieEatSprite3;
extern SpriteType* regularZombieEatSprite4;

extern SpriteType* bucketZombieSprite;
extern SpriteType* bucketZombieSprite2;
extern SpriteType* bucketZombieSprite3;
extern SpriteType* bucketZombieSprite4;

extern SpriteType* bucketZombieEatSprite;
extern SpriteType* bucketZombieEatSprite2;

extern SpriteType* newspaperZombieSprite;
extern SpriteType* newspaperZombieSprite2;
extern SpriteType* newspaperZombieSprite3;

extern SpriteType* newspaperZombieEatSprite;
extern SpriteType* newspaperZombieEatSprite2;

extern SpriteType* polevaultZombieRunSprite;
extern SpriteType* polevaultZombieRunSprite2;
extern SpriteType* polevaultZombieRunSprite3;
extern SpriteType* polevaultZombieRunSprite4;

extern SpriteType* polevaultZombieEatSprite;
extern SpriteType* polevaultZombieEatSprite2;

extern SpriteType* polevaultZombieJumpSprite;

extern SpriteType* polevaultZombieWalkSprite;
extern SpriteType* polevaultZombieWalkSprite2;
extern SpriteType* polevaultZombieWalkSprite3;
extern SpriteType* polevaultZombieWalkSprite4;

extern SpriteType* jackZombieSprite;
extern SpriteType* jackZombieSprite2;
extern SpriteType* jackZombieSprite3;
extern SpriteType* jackZombieSprite4;

extern SpriteType* footballZombieSprite;
extern SpriteType* footballZombieSprite2;
extern SpriteType* footballZombieSprite3;
extern SpriteType* footballZombieSprite4;

extern SpriteType* footballZombieEatSprite;
extern SpriteType* footballZombieEatSprite2;

extern SpriteType* flagZombieSprite;
extern SpriteType* flagZombieSprite2;
extern SpriteType* flagZombieSprite3;
extern SpriteType* flagZombieSprite4;

extern SpriteType* flagZombieEatSprite;
extern SpriteType* flagZombieEatSprite2;

extern SpriteType* coneZombieSprite;
extern SpriteType* coneZombieSprite2;
extern SpriteType* coneZombieSprite3;
extern SpriteType* coneZombieSprite4;

extern SpriteType* coneZombieEatSprite;
extern SpriteType* coneZombieEatSprite2;

//button sprites
extern SpriteType* vsEnglish;
extern SpriteType* vsSpanish;
extern SpriteType* campaignEnglish;
extern SpriteType* campaignSpanish;
extern SpriteType* languageEnglish;
extern SpriteType* languageSpanish;

//seed packet sprites
extern SpriteType* peashooterPacket;
extern SpriteType* sunflowerPacket;
extern SpriteType* snowPeaPacket; 
extern SpriteType* repeaterPacket;
extern SpriteType* chomperPacket; 
extern SpriteType* potatoMinePacket; 
extern SpriteType* cherryBombPacket;
extern SpriteType* wallNutPacket;
extern SpriteType* seedPacketGraySprite;
extern SpriteType* seedPacketSprites[8];

//button sounds
extern Sound* menuSound;
extern Sound* plantingSound;

// other sounds
extern Sound* peaSound;
extern Sound* chompSound;	//for when chomper bites zombie
extern Sound* explosionSound;
extern Sound* sunSound;
extern Sound* biteSound; //for when zombie bites plant
extern Sound* brainsSound;

//language
extern uint8_t lang; //0 = eng, 1 = esp	
	

//sfx/music filenames or whatever we are doing
//TO-DO

//LawnMower things
extern SpriteType* lmSprite;
extern Sound* lmSound;

//Buttons
extern CampaignButton* singlePlayer;
extern VsButton* multiPlayer;
extern LanguageButton* language;

extern SeedPacket* peaSeed;
extern SeedPacket* sunSeed;
extern SeedPacket* snowSeed;
extern SeedPacket* repSeed;
extern SeedPacket* chompSeed;
extern SeedPacket* mineSeed;
extern SeedPacket* bombSeed;
extern SeedPacket* wallSeed;
	
//LawnMowers
extern LawnMower* LM1;
extern LawnMower* LM2;
extern LawnMower* LM3;
extern LawnMower* LM4;
extern LawnMower* LM5;


//Music classes
extern Sound* menuMusic;
extern Sound* levelMusic;
//TO-DO

//Game Object lists
extern GameObject* btnArr1[4];
extern GameObject* btnArr2[8];
//GameObject* btnArr3[16] = {};

extern GameObject* lmwArr[5];

extern GameObjectList* menuButtons;
extern GameObjectList* singlePlayerButtons;
//GameObjectList* multiPlayerButtons = new GameObjectList(btnArr3);

extern GameObjectList* lawnMowers;


//scenes
extern Scene* menu;
extern Scene* campaign;
//Scene* vsmode = new Scene(multiPlayerButtons, lawnMowers, lawnBackground, levelMusic);

extern Sound* s;
extern uint8_t soundBuffer[24000];
extern Scene* currentScene;

//for if jack zombie explodes
extern int screenWipe;
extern int GameTime;

//global functions
void loadScene(Scene* s);
void globalInits(void);


#endif
