#include "RCBuildConfig.h"


Native::Recast::RCBuildConfig::RCBuildConfig()
{
	TileSize				=	0;
	BorderSize				=	0;
	CellSize				=	0.30f;
	CellHeight				=	0.20f;
	WalkableSlopeAngle		=	45.0f;
	WalkableHeight			=	2.0f;
	WalkableClimb			=	0.9f;
	WalkableRadius			=	0.6f;
	MaxEdgeLen				=	12;
	MaxSimplificationError	=	1.3f;
	MinRegionSize			=	8;
	MergeRegionSize			=	20;
	MaxVertsPerPoly			=	6;
	DetailSampleDist		=	6.0f;
	DetailSampleMaxError	=	1.0f;

	Partition				=	RCPartition::Watershed;

	FilterLedgeSpans				=	true;
	FilterLowHangingWalkableObjects			=	true;
	FilterWalkableLowHeightSpans	=	true;

}

Native::Recast::RCBuildConfig::~RCBuildConfig()
{

}
