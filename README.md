# AnimTexture
bake unity animation to texture, and play from vs

Reference package
https://github.com/redcool/PowerShaderLib.git
https://github.com/redcool/PowerUtilities.git

Features:
1 BakeMesh to texture
	renderObject use AnimTexture.shader
	
2 BakeBones to texture
	render object use BoneTexture.shader

3 Calculate bones matices per frame ,then render object(vertex shader calc bones skin)

Usage:

open scene : AnimatorNoBoneUpdate

1 bake bone texture
	1 select AnimTexture_DRP\OtherRes\RTS Mini Legion Footman\Prefabs_ForBakeAnimTex/Footman_Default.prefab
	2 click[menu] PowerUtilities/AnimTexture/BakeBoneTextureAtlas
		Assets/AnimTexture/AnimTexPath will see:
		boneTexture : transform info
		manifest : animation info

2 create a player
	1 select Footman_Default mecAnim.fbx
	2 click[menu] PowerUtilities/AnimTexture/CreatePlayer_FromSelected
	3 find Footman_Default mecAnim_Animator in hierarchy, find TextureAnimationSetup
		1 animTextureManifest, select "Footman_Default_AnimTextureManifest"
		2 Animator Controller ,select AnimTexSimpleController_noClip
		3 AnimTextureMats,add 1 material,select AnimTexture_BoneTexture_DRP
		4 click SetupAnimTexture

3 open instancing:
	1 TextureAnimation check "isUpdateBlock"
	2 material check "enable gpu instancing"
play it and check framedebugger