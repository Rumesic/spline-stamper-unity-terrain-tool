# Spline Stamper | Unity Terrain Extention Tool 
## Final Major Project | Games Art @ Royal Leamington Spa College | Work in Progress
Spline Stamper is a Unity Terrain extension tool that allows you to quickly create paths, ramps, rivers and many other things using Native Unity Terrain.
Add, delete, insert and move points to get the shape you want, the Terrain will automatically adjust both its Heightmap and Alphamap / Splatmap.

![Programming-3D-Artist-Demo-Reel](https://user-images.githubusercontent.com/91887444/136612279-8cd19ed2-b9dd-49f1-a324-1e82c7fc60fd.gif)


# Components
1. **SplineStamper** is the main component that allows you to create Stampers. In should be attached to the Terrain Component.

![image](https://user-images.githubusercontent.com/91887444/135854812-88f72297-d16d-49a1-a897-e7af93ab3e38.png)
- Create New Stamper – Creates a new Stamper as a child of SplineStamper

2. **Stampers** are created as child objects of SplineStamper. They manipulate **TerrainData** and perform most of the necessary calculations. You can have unlimited number of **Stampers** but crossing them can sometimes lead to unexpected results.
You can easily add new stampers by holding CTRL and clicking on the Terrain.

Make sure that the **Gizmos** are enabled in the scene view to edit the Spline.

![image](https://user-images.githubusercontent.com/91887444/135855110-7d8967ee-0ef3-49ea-a2c6-17b810cbd1a7.png)

- Path Width – Controls the width of the path
- Texture Layer – Which Terrain Layer the Stamper will use as main
- Falloff Distance – Adds extra width on both sides to smooth the terrain
- Falloff Texture Layer – Which Terrain Layer the Stamper will use for slopes
- Spacing – Distance between internal points used by **MeshCreator** and **SplineFollower**
- Show Spacing Gizmo – This will enable additional gizmos that allow you to see the spacing. Useful when working with **MeshCreator** or **SplineFollower** but can be resource intensive.
- Toggle Closed – Switches between Open and Closed Loop
- Manual Stamp – Manually stamps the terrain.
- Manual Undo – Manually removes the stamps on the terrain
- Create Spline Follower – Creates a child object capable of following the curve

3. **SplineFollower** is a component that allows you to make objects follow along the curve. You can set the position by changing PointPosition in the inspector and offset it to either side using Width and Side. Setting FacePoint to true will make the object face the center position, no matter which side it is on. You can have as many SplineFollowers as you want under each Stamper. After creating the SplineFollower, simply add GameObjects as child objects to make them follow the curve.

![image](https://user-images.githubusercontent.com/91887444/135855583-17dd91f1-b247-41d0-b222-8c1e8b630d02.png)

- Point Position – Position on the curve the object will Follow normalised to 0-1 range.
- Side – Offset that allows you to define the side and the offset amount
- Width- Maximum offset allowed
- Face Point – If set to on, the object will LookAt the center position.
- Display Gizmos – Visualise the SplineFollower position

![image](https://user-images.githubusercontent.com/91887444/135855762-eb1364b7-dd96-452b-8fdf-2b01b4997454.png)

4. **MeshCreator** component allows you to generate a mesh based on the curve you define. Useful for creating rivers and mesh-based paths such as railroads. It will update automatically with each change if “AutoUpdate” is set to on. It should be added on the same GameObject with the Stamper component. Its resolution is based on the Spacing from Stamper component. You can apply any material / shader to it.

![image](https://user-images.githubusercontent.com/91887444/135856005-10b2a34f-2c13-4f21-b48d-97f8135275fa.png)

- Extra Width – Extrudes the mesh by defined amount to cover additional area.
- Height Offset – Offsets the mesh on Y axis.
- Auto Update – If set to on mesh will be updated on each curve change.

![image](https://user-images.githubusercontent.com/91887444/135856103-2a7150c7-6f8e-458a-a60f-7356890be327.png)

# Known Issues
- Heightmap Resolution and Control Texture Resolution of the Terrain can be changed dynamically, but only Ascending. Decreasing either of the two will break stamperData.
- Mesh created by MeshCreator has broken UVs on some points and inverted Normals, requiring emission texture to be added.
- Overlaping paths might lead to unxpected results after reloading the scene. Probably wrong Undo calculations.

# Credits
Big thanks to [Sebastian Lague](https://github.com/SebLague) for his tutorial on [2D Curve editor](https://www.youtube.com/watch?v=RF04Fi9OCPc&list=PLFt_AvWsXl0d8aDaovNztYf6iTChHzrHP) which allowed me to start working on this tool.
