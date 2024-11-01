# ASE-ShaderGUI
A custom shader GUI with surface options and some drawers.
![dd](https://github.com/user-attachments/assets/67ab5ff7-4a84-4822-80a4-031c663aa33d)
#### Key Features
+ Surface Options see [here](https://github.com/fisekoo/ASE-ShaderGUI?tab=readme-ov-file#surface-options-setup-for-ase) for setup. (Surface Type, Render Face, Depth Write, Depth Test, Alpha Clip, Cast Shadows)
+ Easy to implement to your custom GUI with public functions for surface options.
+ Useful animatable properties.
#### Drawers
Usage: Create a property, go to attributes, at the bottom tick Custom, add an attribute. (Don't forget to tick auto-register for dummy properties like headers and lines.)

<img src="https://github.com/user-attachments/assets/c26bd52c-b029-43bc-8999-a2c3b1d79b33" width=40% height=40%>
 
+ ##### Gradient(resolution, hdr) (Texture property)
    | Parameter | Description |
    | --- | --- |
    | resolution | resolution of gradient texture that will be created |
    | hdr (bool) | yeah. |
+ ##### BoxHeader(fontSize) (float property)
    | Parameter | Description |
    | --- | --- |
    | fontSize | you guessed it. |
+ ##### Line(r,g,b,thickness) (float property)
    | Parameter | Description |
    | --- | --- |
    | r,g,b | 0-255 channel. |
    | thickness | i wonder what it is. |
+ ##### Vector(vecType) (Vector property)
    | Parameter | Description |
    | --- | --- |
    | vecType | 2 if vector2, 3 if vector3. |
+ ##### Range(rangemin, rangemax) (Vector2 property)
    | Parameter | Description |
    | --- | --- |
    | rangemin | min value of vec2 |
    | rangemax | max value of vec2 |
## Surface Options setup for ASE
  + First, create these properties. Don't forget to auto-register and add Hide in Inspector attribute to them. (multiply your alpha threshold with _ALPHATEST_ON before you plug in, that's why it's registered.)
    http://paste.amplify.pt/view/raw/3962eb69
    ![image](https://github.com/user-attachments/assets/299b9dae-2839-47cf-9c8a-fef4d44af66f)
  + Go to SubShader and click the little gray dot next to cull mode, and select Cull property.
    ![image](https://github.com/user-attachments/assets/b66b74b2-9b8c-4d48-80cd-594fe0e7c867)
  + Go to pass section and assign ZWrite, ZTest, Src, Dst like you assigned cull.
    ![image](https://github.com/user-attachments/assets/7fb4d4cc-3c89-4f17-8b4f-16df3e0aec87)

  + Don't forget to use Fisekoo.BaseShaderGUI, or you can use UnityEditor.ShaderGraphUnlitGUI/UnityEditor.ShaderGraphLitGUI if you'd like. (you can still use all the drawers dw.)

    ![image](https://github.com/user-attachments/assets/85a2d2f8-bb77-40bc-acb2-f19e2c588e43)
