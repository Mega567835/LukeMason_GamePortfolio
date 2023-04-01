// put implementations for functions, explain how it works
// put your names here, date
#include <stdint.h>
#include "inc/tm4c123gh6pm.h"
#include "DAC.h"

Sound* sounds[NUM_SOUNDS] = {0};

extern "C" void DisableInterrupts(void);
extern "C" void EnableInterrupts(void);
Sound::Sound(const uint8_t* soundBuffer, uint16_t bufferSize){
	this->soundBuffer = soundBuffer;
	this->bufferPtr = 0;
	this->bufferSize = bufferSize;
	this->ind = -1;
}

Sound::Sound(){
	this->soundBuffer = 0;
	this->bufferPtr = 0;
	this->bufferSize = 0;
	this->ind = -1;
}

void Sound::increment(){
	if(this->bufferPtr >= this->bufferSize-1){
		this->bufferPtr = 0;
		sounds[this->ind] = 0;
		this->ind = -1;
	}
	else{
		this->bufferPtr++;
	}
}

uint8_t Sound::getSample(){
	return this->soundBuffer[this->bufferPtr];
}

void Sound::play(){
	// Adds to array
	if(this->ind == -1){
		for(int i=0; i<NUM_SOUNDS; i++){
			if(sounds[i] == 0){
				this->ind = i;
				sounds[this->ind] = this;
				this->bufferPtr = 0;
				break;
			}
		}
	}
}

// **************DAC_Init*********************
// Initialize 6-bit DAC, called once 
// Input: none
// Output: none
void DAC_Init(void){
	SYSCTL_RCGCGPIO_R |= 0x02;
	volatile uint32_t delay = SYSCTL_RCGCGPIO_R;
	GPIO_PORTB_DIR_R |= 0xFF;	// 3F for 6-bit DAC, FF for 8-bit DAC
	GPIO_PORTB_DEN_R |= 0xFF;
	GPIO_PORTB_PDR_R = 0x00;
	GPIO_PORTB_PUR_R = 0x00;
	GPIO_PORTB_DR8R_R |= 0xFF;
	
	for(int i=0; i<NUM_SOUNDS; i++){
		sounds[i] = 0;	// Initialize sounds array to 0
	}
	
	Timer0_Init(&playAllSounds, 80000000/SAMPLE_RT);
}

// **************DAC_Out*********************
// output to DAC
// Input: 6-bit data, 0 to 63 OR 8-bit data, 0 to 255
// Input=n is converted to n*3.3V/63 OR n*3.3V/255
// Output: none
void DAC_Out(uint32_t data){
	GPIO_PORTB_DATA_R = data&0xFF;
}

void playAllSounds(){
	uint8_t total = 0;
	uint8_t ct = 0;
	for(int i=0; i<NUM_SOUNDS; i++){
		if(sounds[i] != 0){
			uint8_t sample = sounds[i]->getSample();
			total += sample;
			ct++;
			sounds[i]->increment();
		}
	}
	if(ct != 0){
		DAC_Out(total/ct);
	}
}