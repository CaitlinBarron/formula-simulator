// ACFan.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

#include <windows.h>
#include <iostream>
#include <thread>

#include "SharedFileOut.h"
#include "SerialClass.h"

using namespace std;

template <typename T, unsigned S>
inline unsigned arraysize(const T(&v)[S])
{
	return S;
}

struct SMElement
{
	HANDLE hMapFile;
	unsigned char* mapFileBuffer;
};

SMElement m_physics;
SMElement m_static;

bool running;

void initPhysics()
{
	TCHAR szName[] = TEXT("Local\\acpmf_physics");
	m_physics.hMapFile = CreateFileMapping(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, 0, sizeof(SPageFilePhysics), szName);
	if (!m_physics.hMapFile)
	{
		MessageBoxA(GetActiveWindow(), "CreateFileMapping failed", "ACS", MB_OK);
	}
	m_physics.mapFileBuffer = (unsigned char*)MapViewOfFile(m_physics.hMapFile, FILE_MAP_READ, 0, 0, sizeof(SPageFilePhysics));
	if (!m_physics.mapFileBuffer)
	{
		MessageBoxA(GetActiveWindow(), "MapViewOfFile failed", "ACS", MB_OK);
	}
}

void initStatic()
{
	TCHAR szName[] = TEXT("Local\\acpmf_static");
	m_static.hMapFile = CreateFileMapping(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, 0, sizeof(SPageFileStatic), szName);
	if (!m_static.hMapFile)
	{
		MessageBoxA(GetActiveWindow(), "CreateFileMapping failed", "ACS", MB_OK);
	}
	m_static.mapFileBuffer = (unsigned char*)MapViewOfFile(m_static.hMapFile, FILE_MAP_READ, 0, 0, sizeof(SPageFileStatic));
	if (!m_static.mapFileBuffer)
	{
		MessageBoxA(GetActiveWindow(), "MapViewOfFile failed", "ACS", MB_OK);
	}
}

void dismiss(SMElement element)
{
	UnmapViewOfFile(element.mapFileBuffer);
	CloseHandle(element.hMapFile);
}

BOOL WINAPI consoleHandler(DWORD signal) {
	if (signal == CTRL_C_EVENT)
		running = false;
	return TRUE;
}

int _tmain(int argc, _TCHAR* argv[])
{
	if (!SetConsoleCtrlHandler(consoleHandler, TRUE)) {
		printf("\nERROR: Could not set control handler"); 
		return 1;
	}

	if (argc < 2) {
		std::cerr << "Usage: " << argv[0] << " Com Port Name [eg. COM1]" << std::endl;
		return 1;
	}

	char comString[20];
	sprintf_s(comString, "\\\\.\\%s", argv[1]);

	initPhysics();
	initStatic();

	printf("Trying to connect to port: %s\n", argv[1]);

	Serial* SP = new Serial(comString);
	running = false;
	if (SP->IsConnected())
	{
		printf("Successfuly connected to %s\n\n", argv[1]);
		running = true;
	}

	while (running)
	{
		SPageFilePhysics* pf = (SPageFilePhysics*)m_physics.mapFileBuffer;

		float speed = pf->speedKmh;
		float steerAngle = pf->steerAngle;

		SPageFileStatic* sf = (SPageFileStatic*)m_static.mapFileBuffer;
		// car model could be used to determine if you want to run the fan or not
		char * carModel =  sf->carModel;

		int fanSpeed;
		if (speed < 1)
		{
			// initial/stationary esc value
			fanSpeed = 30;
		}
		else {
			// adjust this to fit your ESC/motor and liking
			// 61 was the value my motor started running
			// 350 was my max speed (in kmh)
			// 50 was an arbitary multiplier to normalise the output values between ~61-100
			// also added a bit of variance with steerAngle so that when you turn in you get a little less "wind"
			fanSpeed = 61 + (int)((speed / 350) * 50 - (3 * abs(steerAngle)));
		}

		printf("Current ESC value: %d\r", fanSpeed);

		char buffer [50];
		int n = sprintf_s(buffer, "%d\n", fanSpeed);

		if (SP->IsConnected()) 
			SP->WriteData(buffer, n);

		// 10Hz
		std::this_thread::sleep_for(std::chrono::milliseconds(100));
	}

	dismiss(m_physics);
	return 0;
}