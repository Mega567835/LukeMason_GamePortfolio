
#include "Game.h"

//
// Globals, Animations, Loading, and Initializations set up here
// Skipping that code for brevity sake
//

//Beginning of the game scene creation
Scene* currentScene = menu;

//initializing game cursors, game object lists, and other game values
Scene::Scene(GameObjectList* but, GameObjectList* lwm, const uint16_t* bg, uint8_t hasGrid){
		this->Buttons = but;
		this->inputRate = sceneInputRate;
		this->inputTimer = sceneInputRate;
		this->Plants = new GameObjectList();
		this->Zombies = new GameObjectList();
		if(lwm != 0)
			this->Lawnmowers = lwm;
		else
			this->Lawnmowers = new GameObjectList();
		this->Projectiles = new GameObjectList();
		this->backgroundBMP = bg;
		this->sunRate = sunProductionRate;
		this->sunTimer = 0;
		this->sunAmount = 200;
		this->select = new SelectCursor(but);
		this->hasGrid = hasGrid;
		this->zombieTimer = 1;
		if(hasGrid)
			this->planter = new GridCursor();
		else
			this->planter = 0;
}
void Scene::refresh(){
	if(this->select != 0)
	{
			this->select->refresh();
		if(currentScene != this){
			return;
		}
	}
	if(this->planter != 0){
			this->planter->refresh();
	}
	this->Buttons->refresh();
	this->Plants->refresh();
	this->Zombies->refresh();
	this->Lawnmowers->refresh();
	this->Projectiles->refresh();
}

//calls time progression on all gameobject lists, and checks on the two cursors as well (plant selection and plant placement)
//also handles time for zombie spawning
void Scene::tick(){
	Random32();
	this->Buttons->tick();
	this->Plants->tick();
	this->Zombies->tick();
	this->Lawnmowers->tick();
	this->Projectiles->tick();
	
	GameTime++;
	
	//if the game has started
	if(this->hasGrid){
		this->planter->redraw = 1;
		
		//checking if we can spawn a zombie
		if(this->zombieTimer <= 0){
			this->zombieTimer = 500 - GameTime/7;
			if (this->zombieTimer < 40) this->zombieTimer = 40;
			int zombienum = Random32()%8;
			if(zombienum > 5 && Random32() % 3 > 0) zombienum = regularZombieID;
			this->spawnZombie(zombienum, Random32()%5 + 1);
		}
		else this->zombieTimer--;
	
		//checking if we can spawn a sun to collect
		if(this->sunTimer!=0) this->sunTimer--;
		else{
			this->Lawnmowers->redrawSet();
			this->spawnProjectile(sunID, Random()/2 + 20, 100, 0);
			sunTimer = sunRate;
		}
		//handles cursor movement
		if(this->inputTimer == 0){
			JoyX = 0;
			JoyY = 0;
			this->inputTimer = this->inputRate;
		}
		else inputTimer--;
	}
}

//sets the scene and draws the background
void loadScene(Scene* s){
	currentScene = s;
	Display_DrawBitmap(0, 0, s->backgroundBMP, 160, 128);
}
	
//Creating the sprite objects when game loads;
SpriteType::SpriteType(const uint16_t* bmp, uint8_t width, uint8_t length, SpriteType* nextSprite){
	this->bmp = bmp;
	this->length = length;
	this->width = width;
	this->nextSprite = nextSprite;
}


//Gameobjects include entities, lawnmowers, sun, and UI elements
GameObject::GameObject(SpriteType* sp, Sound* sfx, uint8_t x, uint8_t y){
	this->sprite = sp;
	this->soundFX = sfx;
	this->x = x;
	this->y = y;
	this->oldx = x;
	this->oldy = y;
	this->previousSprite = sp;
	this->redraw = 1;
}

//Some gameobjects are stuck to a lane of grass (entities and lawnmowers)
GameObject::GameObject(SpriteType* sp, Sound* sfx, uint8_t x, uint8_t y, uint8_t lane){
	this->sprite = sp;
	this->soundFX = sfx;
	this->x = x;
	this->y = y;
	this->redraw = 1;
	this->lane = lane;
	this->oldx = x;
	this->oldy = y;
	this->previousSprite = sp;
}

//calls advance to check state changes, and if sprite has moved or animated then redraw it
void GameObject::refresh(){
		this->oldx = this->x;
		this->oldy = this->y;
		this->previousSprite = this->sprite;
		advance();
		if(this->redraw == 1){	
			unrender();
			render();
			this->redraw = 0;
		}
}

//Renders the sprite in its position
void GameObject::render(){
	Display_RenderSprite(this->x, this->y, this->sprite->bmp, this->sprite->width, this->sprite->length, transparentColor, currentScene->retBG());
}

//Gameobjects need to unrender before rendering again so we don't duplicate sprites upon movement
void GameObject::unrender(){
	if(this->redraw == 0){
		this->previousSprite = this->sprite;
		this->oldx = this->x;
		this->oldy = this->y;
	}
	Display_UnrenderSprite(this->oldx, this->oldy, this->previousSprite->bmp, this->previousSprite->width, this->previousSprite->length, currentScene->retBG());
}

void GameObject::advance(){
	//virtual, will be inherited - checks state changes
}

//governs what happens when time passes - will be inherited
void GameObject::tick(){}
//all game objects have functionality on collision
int GameObject::collided(){return 0;}


//This class handles anything with health and animation
Entity::Entity(SpriteType* sp, Sound* sfx, uint8_t xpos, uint8_t ypos, 
							uint8_t hp, uint8_t anim, uint8_t hostl, uint8_t lane)
							: GameObject(sp, sfx, xpos, ypos, lane)
{
		this->health = hp;
		this->animationTime = anim;
		this->animationTimer = anim;
		this->hostile = hostl;
}

//This handles the passing of time for entities (animation)
void Entity::tick(){
	if(this->animationTimer > 0) this->animationTimer--;
}

//Checks animation timer and flips sprites if done
void Entity::advance(){
	if(this->animationTimer == 0){
		this->sprite = this->sprite->nextSprite;
		this->animationTimer = this->animationTime;
		this->redraw = 1;
	}
}

//Entities all have health
void Entity::hurt(uint8_t damage){
	this->health-=damage;
}


//Handles certain lists of gameobjects to check during gameplay loops, handled in initialization
GameObjectList::GameObjectList(){
	this->indexPtr = 0;
}
GameObjectList::GameObjectList(GameObject** GOlist){
	this->indexPtr = 0;
	for(int i=0; GOlist[i] != 0; i++){
		this->GOAdd(GOlist[i]); 
	}
}
		//Destructor
GameObjectList::~GameObjectList(){
	for(int i = 0; i < indexPtr; i++){
		delete objects[i];
	}
}
//Add Object
void GameObjectList::GOAdd(GameObject* add){
	this->objects[indexPtr] = add;
	this->indexPtr++;
}
//Access Object
GameObject* GameObjectList::operator[](uint8_t i){
	return this->objects[i];
}
//Remove Object at index - when entities die/get collected
void GameObjectList::GORmv(uint8_t i){
	for(int j = i; j < indexPtr; j++){
		objects[j] = objects[j+1];
	}
	objects[this->indexPtr] = 0;
	this->indexPtr--;
}
//Called to render objects when game initialized
void GameObjectList::redrawSet(){
	for(int i = 0; i<indexPtr; i++){
		objects[i]->redraw = 1;
	}
}
//Calls refresh on objects contained in list
void GameObjectList::refresh(){
	int16_t i = this->indexPtr-1; 
	while(i>=0){
		this->objects[i]->refresh();
		i--;
	} 
}
//calls time advancing on all objects in list
void GameObjectList::tick(){
	int16_t i = this->indexPtr-1;
	while(i>=0){
		this->objects[i]->tick();
		i--;
	}
}
//how the seed packet selector is initialized - placement is similar
SelectCursor::SelectCursor(GameObjectList* gos){
	this->buttonIndex = 0;
	this->targetButtons = gos;
	this->redraw = 1;
	this->button = this->targetButtons->objects[this->buttonIndex];
	this->oldButton = this->button;
}

//more code not included - cursor and other UI updates 