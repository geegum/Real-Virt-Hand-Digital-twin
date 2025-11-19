# 🦾 Remote Robotic Handling via VR-Based Teleoperation
### Applications in Radioactive Waste Management (LLRW)

> **Author:** Gigeom Lee (이기검)  
> **Affiliation:** UST-KAERI School (Dept. of AI), Korea Atomic Energy Research Institute (KAERI)

![Project Poster](슬라이드1.PNG) 
*(Please ensure the image file name matches the one in your repository)*

---

## 📖 Overview
This project implements an **immersive teleoperation system** designed for high-risk tasks, specifically the handling of **Low-Level Radioactive Waste (LLRW)**. 

Traditional 2D monitor-based teleoperation suffers from a lack of depth perception and limited field of view. To overcome this, we developed a **Digital Twin-based VR interface** that allows the operator to control a **Doosan A0509 collaborative robot** intuitively using hand gestures. The system reconstructs the remote work environment in real-time 3D using **Azure Kinect**, providing the operator with a high sense of presence ("Immersiveness").

---

## ✨ Key Features

### 1. Immersive VR Interface
- **1st Person Perspective:** Provides a stereoscopic view of the remote site through **Meta Quest (HMD)**.
- **Hand Tracking:** Maps the operator's hand movements directly to the robot's end-effector without physical controllers.

### 2. Real-Time Digital Twin
- **3D Point Cloud Streaming:** Utilizes **Azure Kinect** depth cameras to scan the workspace.
- **Unity Integration:** Renders the scanned environment and robot model synchronously in the Unity engine.

### 3. Advanced Robot Control
- **Inverse Kinematics (IK):** Implements the **Newton-Raphson Method** to calculate joint angles in real-time based on hand position.
- **Safety Mechanisms:** Includes collision detection and torque estimation. If external torque exceeds a threshold (e.g., collision), the robot enters "Safety Mode."

---

## 🛠️ System Architecture

The system consists of two main loops operating in parallel:

| Loop Type | Description | Technologies |
| :--- | :--- | :--- |
| **Robot Control Loop** | Handles motion planning, kinematics, and hardware communication. | C++, C#, Doosan API, Newton-Raphson |
| **3D Environment Loop** | Manages point cloud data acquisition, rendering, and VR visualization. | Unity, Azure Kinect SDK, Shader Graph |

### Hardware Setup
* **Robot:** Doosan Robotics A0509 (Cooperative Robot) + Robotic Gripper
* **VR Headset:** Meta Quest (Hand Tracking enabled)
* **Vision Sensor:** Microsoft Azure Kinect DK (Depth & RGB)
* **Computing:** High-Performance Workstation (NVIDIA RTX Series for Point Cloud Processing)
* **Network:** Wired/Wireless Mesh Network for low-latency transmission

---

## 🧪 Experimental Results
**Task:** Sorting simulated radioactive waste (gloves, samples) in a remote environment.
* **Setup:** The operator controlled the robot from a separate "Control Room" visually isolated from the "Robot Room."
* **Performance:** Successfully sorted **6 items in 2 minutes 30 seconds**.
* **Feedback:** The system demonstrated high spatial awareness and intuitive handling compared to traditional 2D methods.

---

## 📬 Contact
If you have any questions or collaboration suggestions, please feel free to contact me.

* **Name:** Gigeom Lee (이기검) / AI Engineer & Master's Student
* **Email:** [igigeom@kaeri.re.kr](mailto:igigeom@kaeri.re.kr)
* **Phone:** 042-868-2247
* **Lab:** Visualization of Mobile Robot Lab, KAERI

---
*This project was presented at KRS 2025 (Korea Robotics Society Annual Conference), Jeju Int. Convention Center.*
