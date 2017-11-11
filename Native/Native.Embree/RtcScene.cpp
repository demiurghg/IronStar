
#include "local.h"

namespace Native {
namespace Embree {

	RtcScene::RtcScene( Rtc ^ rtc, SceneFlags sceneFlags, AlgorithmFlags algorithmFlags )
	{
		this->scene = rtcDeviceNewScene( rtc->device, (RTCSceneFlags)sceneFlags, (RTCAlgorithmFlags)algorithmFlags );
		RtcException::CheckError();
	}



	RtcScene::~RtcScene()
	{
	}

}}