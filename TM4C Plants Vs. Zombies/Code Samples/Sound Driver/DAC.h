// put prototypes for public functions, explain what it does
// put your names here, date
#ifndef __DAC_H__ // do not include more than once
#define __DAC_H__
#include <stdint.h>
#include "Display.h"
#include "Timer0.h"

#define SAMPLE_RT 4000
#define BIT_DEPTH 8
#define NUM_SOUNDS 7

// Sound stored on SD Card
class Sound{
	protected:
		const uint8_t* soundBuffer;	// Sound buffer
		uint16_t bufferPtr;	// Current location in buffer
		uint16_t bufferSize;	// Size of buffer
		int8_t ind;	// index in global sound array
	
		// Load sound effect into local memory
		void loadFile();
	public:
		// Constructor
		Sound(const uint8_t* soundBuffer, uint16_t bufferSize);
	
		// Constructor
		Sound();
		
		// Destructor
		~Sound();
	
		// Go to next sample. If reached end, free buffer and close file but DON'T destroy sound.
		void increment();
	
		// Get current sample
		uint8_t getSample();
	
		//plays the sound (I referenced this in a lot of classes, so implement it as play lol)
		void play();
};

// Takes in a Sound* and adds it to the global array of sounds
void Sound_Init(Sound* s);

// **************DAC_Init*********************
// Initialize 6-bit DAC, called once 
// Input: none
// Output: none
void DAC_Init(void);

// **************DAC_Out*********************
// output to DAC
// Input: 6-bit data, 0 to 63 OR 8-bit data, 0 to 255
// Input=n is converted to n*3.3V/63 OR n*3.3V/255
// Output: none
void DAC_Out(uint32_t data);

// Play all sounds in the array
void playAllSounds();

#endif