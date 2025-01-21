----------------------------------------
Pixel Arsenal by Archanor VFX
----------------------------------------

1. Introduction
2. Scaling effects
3. Upgrading to Universal/URP
4. FAQ 
5. Asset Extras
6. End Notes

----------------------------------------
1. INTRODUCTION
----------------------------------------

Effects can be found in the 'Pixel Arsenal/Prefabs' folder. Here they are sorted in 3 main categories: Combat, Environment and Interactive.

In each category folder is a more detailed explanation of what effects you'll find inside.

----------------------------------------
2. SCALING EFFECTS
----------------------------------------

To scale an effect in the scene, you can simply use the default Scaling tool (Hotkey 'R'). You can also select the effect and set the Scale in the Hierarchy.

Please remember that some parts of the effects such as Point Lights, Line Renderers, Trail Renderers and Audio Sources may have to be manually adjusted afterwards.

----------------------------------------
3. Upgrading to Universal / URP
----------------------------------------

Make sure your project is correctly set up to use Universal Pipeline.

Locate the 'Pixel Arsenal\Upgrade' folder, then open and Import the bundled 'Pixel Arsenal URP' unitypackage to your project. This should overwrite the Standard Shaders, custom shaders and Materials.

If you accidentally upgraded, simply re-import the asset from the Package Manager.

----------------------------------------
4. FAQ
----------------------------------------

Q: Particles appear stretched or too thin after scaling
 
A: This means that one of the effects are using a Stretched Billboard render type. Select the prefab and locate the Renderer tab at the bottom of the Particle System. If you scaled the effect up to be twice as big, you'll also need to multiply the current Length Scale by two.

Q: The effects seem to be invisible in Mobile builds

A: This is usually because of Soft Particles being enabled on the particle Materials. In most cases you can select all materials and toggle off Soft Particles.

----------------------------------------
5. ASSET EXTRAS
----------------------------------------

In the 'Pixel Arsenal/Scripts' folder you can find some neat scripts that may further help you customize the effects.

PixelArsenalBeamStatic - A script for making a static interactive beam. Requires you to select prefabs from the Beam effect folder

PixelArsenalLightFade - This lets you fade out lights which are useful for explosions

PixelArsenalLightFlicker - Attach this to a prefab with a Light on it and it will pulse or flicker

PixelArsenalLoopScript - A script that lets you constantly spawn effects

PixelArsenalRotation - A simple script that applies constant rotation to an object

----------------------------------------
6. END NOTES
----------------------------------------

If you need help with anything, please get in touch at https://archanor.com/support.html

Special thanks to:

Jan Jørgensen
Sound fx in this asset was created with BFXR: http://www.bfxr.net/