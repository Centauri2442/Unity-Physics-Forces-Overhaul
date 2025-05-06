# Unity Physics Forces Overhaul
In-development for use in an unannounced game, this system optimizes force calculations at scale for things like wind and river simulations.

# License Notice

2025 CentauriCore LLC, 2025 Yewnyx Studios, 2025 CASCAS! LLC, All rights reserved.

# Overview
In Unity, rigidbody mass distribution is uniform across the entire object, assuming all attached colliders contribute evenly to the total mass. While this works fine in most situations, it can break down when trying to do more complex simulations, especially when cpu frametime is needed to be kept low (Thus limiting the usage of joint based approaches).

The Physics Forces Overhaul system attempts to performantly solve this limitation, allowing more complex physics interactions and also allowing different parts of an object to contain different mass density. For example, a hammer could be simulated with a wooden handle and steel head, with much more accurate simulated movement when force is applied. 

The system also expands on physics interactivity:
- Allows for forces to only be applied to objects from an offset, without any custom AddForceAtPosition() implementation required for each type of system. The amount of force is also adjusted based on the mass density at the specific points that force is being applied to.
- The system can handle hundreds of thousands of force point calculations per-second on mobile (Pixel 9), and millions per-second on Windows. (AMD Ryzen 5950X)
  - This frees up CPU frametime to allow more rigidbodies being simulated at once, as the force calculations are completely off the main thread.

# Performance Showcase

### Downloadable Demos
Demo builds can be found [HERE](https://github.com/Centauri2442/Unity-Physics-Forces-Overhaul/releases/tag/release)

### Mobile - Pixel 9
[![Force System Demo - Pixel 9](http://img.youtube.com/vi/kkWScN1fyQ0/0.jpg)](http://www.youtube.com/watch?v=kkWScN1fyQ0 "Force System Demo - Pixel 9")
### Windows - AMD Ryzen 5950X
[![Force System Demo - Windows](http://img.youtube.com/vi/v5dI8HUqKeM/0.jpg)](http://www.youtube.com/watch?v=v5dI8HUqKeM "Force System Demo - Windows")

# How it works
The system can be broken down into multiple parts:
- **IForceRef**
  - Interface that allows implementation of ForceField sampling onto a script.
- **ForceInteractor**
  - Abstract class that implements IForceRef, and also handles automatic registering of any ForceInteractor object to ForceFields, using OnTrigger events. It also implements patches for OnTrigger event bugs (Such as OnTriggerExit not firing when disabling), and allows for ForceInteractors to have multiple colliders with a single OnTrigger event.
- **ForceField**
  - Base class with a set collider bounds that controls forces within it. This includes systems such as spherical gravity, explosions, wind simulation, and flowing water simulation. Internally, it acts as a 3D vector field that can be sampled from any set of Vector3 point at a cheap cpu cost.

## IForceRef
An interface that acts as the hook for any scripts that wish to sample Vector3 points from a ForceField. It contains the following:
- **ForcePositions {get; }**
  - Property is used by any attached ForceFields to sample Vector3[] world-space positions.
  - Any scripts using the IForceRef interface must implement their own setup for creating or feeding in stored Vector3 positions. Attached ForceField objects will automatically get from this property in the next created ForceJob.

- **OnForceUpdated(Vector3[] forces)**
  - Callback when an array of forces has been calculated. The array size is the same as the last ForcePositions property Vector3[] pulled from the interface.
  - Due to force calculations being handled asynchronously, this is not guaranteed to fire in the same frame.

## ForceInteractor
Force Interactors allow any inherited script to automatically attach themselves to a ForceField using OnTrigger events, with multiple colliders. 

Currently has 2 inheritors:
- **ForceObject**
  - Acts as a full overhaul of rigidbody objects, allowing for more complex physics interactions and connects the rigidbody into the force system.
  - **ObjectForcePoint**
    - LocalPosition
    - MassPercentage (Percentage of the object's total mass this point contains)
    - Acts as a simulation point. This means that only points that are within a ForceField have forces applied, rather than the entire object.
  - **Automatic Point Generation**
    - Points can be placed manually, or can be placed automatically at a set density.
- **ForceParticleSystem**
  - WIP implementation of a ParticleSystem that acts as objects within attached ForceFields.
 
### ForceObject Example
![image](https://github.com/user-attachments/assets/d73a1c82-fd33-4f49-92ab-c514c3e98062)


## ForceField
ForceFields act as a base class to control different forms of 3D vector fields. It uses Unity Jobs to run all force simulations inside worker threads, freeing up main thread for other systems and allowing async calculations.

ForceField's can have their points sampled directly in a single frame, or have objects added to the ForceJob queue.
- **Direct**
  - Allows force calculations to be used immediately, but doesn't leverage async processing.
- **ForceJob Queue**
  - Simulates large sets of force points asynchronously inside worker threads, and returns data to reach respective IForceRef when completed. Only ForceInteractor's automatically register themselves to this system, any other scripts using IForceRef must manually attach themselves when they wish to receive force data.

ForceField's can apply forces in 2 ways:
- **Gravity**
  - Applies force to the center of mass of the object and applies force evenly no matter the object's mass.
- **Force**
  - Applies force to individual force points on an object, respecting the object's mass.

Currently the ForceField has 2 inheritors:
- **ForceFieldSphere**
  - Pulls force towards/away from a point in space at an input 
- **ForceFieldZone**
  - Allows any amount of ForcePoints, each with their own force direction and intensity. Can be used to create more complex force movements such as a wind zone.

### ForceFieldSphere Example
![image](https://github.com/user-attachments/assets/dd9ac314-b7c5-4157-9f41-4bf88f590d6e)

# Performance Notes For High Load
- Due to how ForceJob queuing works, it is more performant to queue a large amount of positions at once in a single array rather than a large amount of smaller arrays. This is due to the GC allocations when creating a job. The performance hit is minimal, but can be noticeable at high load.
  - Will fix this in a future version.

# Future Feature-List
- Dynamic drag based on mesh orientation and surface area
- ForceInteractor overhaul
  - Replace ObjectForcePoints with ForceVolumes, which would allow elements of the object to be more easily assigned a density/mass percentage. Would also allow easier assignment of material densities to a whole chunk of the object.
- Force Modifiers
  - Allows ForceFields and ForceInteractors to have overrides and tweaks able to be added/removed during runtime
- Water ForceField
  - River Splines
- Overhaul job creation code to decrease amount of GC allocations at high load.
  - Decrease Vector3 array allocations
  - Improve handling of collider bounds, handling more setup inside the job instead of on the main thread
