#include <windows.h>
#include "Local.h"

using namespace Fusion;


TimeVal getPerfTime()
{
	__int64 count;
	QueryPerformanceCounter((LARGE_INTEGER*)&count);
	return count;
}

int getPerfTimeUsec(const TimeVal duration)
{
	static __int64 freq = 0;
	if (freq == 0)
		QueryPerformanceFrequency((LARGE_INTEGER*)&freq);
	return (int)(duration * 1000000 / freq);
}

//-------------------------------------------------------------	

BuildContext::BuildContext()
{
	resetTimers();
}

// Virtual functions for custom implementations.
void BuildContext::doResetLog()
{
}

void BuildContext::doLog(const rcLogCategory category, const char* msg, const int len)
{
	auto str = gcnew System::String(msg, 0, len);

	switch (category)
	{
	case RC_LOG_PROGRESS:	Log::Message(str);	break;
	case RC_LOG_WARNING:	Log::Warning(str);	break;
	case RC_LOG_ERROR:		Log::Error(str);	break;
	}
}

void BuildContext::doResetTimers()
{
	for (int i = 0; i < RC_MAX_TIMERS; ++i)
		m_accTime[i] = -1;
}

void BuildContext::doStartTimer(const rcTimerLabel label)
{
	m_startTime[label] = getPerfTime();
}

void BuildContext::doStopTimer(const rcTimerLabel label)
{
	const TimeVal endTime = getPerfTime();
	const TimeVal deltaTime = endTime - m_startTime[label];
	if (m_accTime[label] == -1)
		m_accTime[label] = deltaTime;
	else
		m_accTime[label] += deltaTime;
}

int BuildContext::doGetAccumulatedTime(const rcTimerLabel label) const
{
	return getPerfTimeUsec(m_accTime[label]);
}






