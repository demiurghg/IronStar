
local builder = require('content-builder')

builder.reset();

builder.add('ubershader', 'shaders\*.hlsl');
builder.add('texture'	, 'textures\*.tga');
builder.add('texture'	, 'textures\*.jpg');
builder.add('texture'	, 'textures\*.png');
builder.add('copy'		, 'models\*.json' );
builder.add('lua'		, 'scripts\*.lua' );

builder.build();

builder.rebuild();
