using System;
using System.Collections.Generic;
using UnityEngine;

namespace PilotAssistant.Utility
{
    public class FlightData
    {
        public Vessel Vessel { get; private set; }

        public double RadarAlt { get; private set; }
        public double Pitch    { get; private set; }
        public double Roll     { get; private set; }
        public double Yaw      { get; private set; }
        public double AoA      { get; private set; }
        public double Heading  { get; private set; }

        public double ProgradeHeading { get; private set; } //
        public double VertSpeed       { get; private set; } //
        public double Acceleration    { get; private set; } //

        private double oldSpeed = 0;
        private Vector3d vesselFacingAxis = Vector3d.zero;

        public Vector3d LastPlanetUp { get; private set; } //
        public Vector3d PlanetUp     { get; private set; }
        public Vector3d PlanetNorth  { get; private set; }
        public Vector3d PlanetEast   { get; private set; }

        public Vector3d SurfVelForward { get; private set; }
        public Vector3d SurfVelRight   { get; private set; }

        public Vector3d SurfVesForward { get; private set; }
        public Vector3d SurfVesRight   { get; private set; }

        public Vector3d Velocity { get; private set; } //

        public Vector3 ObtRadial { get; private set; }
        public Vector3 ObtNormal { get; private set; }
        public Vector3 SrfRadial { get; private set; }
        public Vector3 SrfNormal { get; private set; }

        public FlightData(Vessel v) { Vessel = v; }

        public void UpdateAttitude()
        {
            vesselFacingAxis = Vessel.transform.up;

            // TODO: Update comment later
            // // 4 frames of reference to use. Orientation, Velocity, and both of the previous parallel to the surface
            // // Called in OnPreAutoPilotUpdate. Do not call multiple times per physics frame or the "lastPlanetUp" vector will not be correct and VSpeed will not be calculated correctly
            // // Can't just leave it to a Coroutine becuase it has to be called before anything else
            RadarAlt = Vessel.altitude - (Vessel.mainBody.ocean ? Math.Max(Vessel.pqsAltitude, 0) : Vessel.pqsAltitude);
            Velocity = Vessel.rootPart.Rigidbody.velocity + Krakensbane.GetFrameVelocity();
            Acceleration = Acceleration * 0.8 + 0.2 * (Vessel.srfSpeed - oldSpeed) / TimeWarp.fixedDeltaTime; // vessel.acceleration.magnitude includes acceleration by gravity
            VertSpeed = Vector3d.Dot((PlanetUp + LastPlanetUp) / 2, Velocity);

            // surface vectors
            LastPlanetUp = PlanetUp;
            PlanetUp     = (Vessel.rootPart.transform.position - Vessel.mainBody.position).normalized;
            PlanetEast   = Vessel.mainBody.getRFrmVel(Vessel.findWorldCenterOfMass()).normalized;
            PlanetNorth  = Vector3d.Cross(PlanetEast, PlanetUp).normalized;

            // Velocity forward and right parallel to the surface
            SurfVelForward = Vector3.ProjectOnPlane(Vessel.srf_velocity, PlanetUp).normalized;
            SurfVelRight = Vector3d.Cross(PlanetUp, SurfVelForward).normalized;
            // Vessel forward and right vetors, parallel to the surface
            SurfVesRight = Vector3d.Cross(PlanetUp, vesselFacingAxis).normalized;
            SurfVesForward = Vector3d.Cross(SurfVesRight, PlanetUp).normalized;

            ObtNormal = Vector3.Cross(Vessel.obt_velocity, PlanetUp).normalized;
            ObtRadial = Vector3.Cross(Vessel.obt_velocity, ObtNormal).normalized;
            SrfNormal = Vector3.Cross(Vessel.srf_velocity, PlanetUp).normalized;
            SrfRadial = Vector3.Cross(Vessel.srf_velocity, SrfNormal).normalized;

            Pitch = 90 - Vector3d.Angle(PlanetUp, vesselFacingAxis);
            Heading = -1 * Vector3d.Angle(-SurfVesForward, -PlanetNorth) * Math.Sign(Vector3d.Dot(-SurfVesForward, PlanetEast));
            if (Heading < 0)
                Heading += 360; // offset -ve heading by 360 degrees

            ProgradeHeading = -1 * Vector3d.Angle(-SurfVelForward, -PlanetNorth) * Math.Sign(Vector3d.Dot(-SurfVelForward, PlanetEast));
            if (ProgradeHeading < 0)
                ProgradeHeading += 360; // offset -ve heading by 360 degrees

            Roll = Vector3d.Angle(SurfVesRight, Vessel.ReferenceTransform.right) * Math.Sign(Vector3d.Dot(SurfVesRight, -Vessel.ReferenceTransform.forward));

            if (Vessel.srfSpeed > 1)
            {
                // Velocity vector projected onto a plane that divides the airplane into left and right halves
                Vector3d AoAVec =
                    (Vector3d)vesselFacingAxis * Vector3d.Dot(vesselFacingAxis, Vessel.srf_velocity.normalized) +
                    (Vector3d)Vessel.ReferenceTransform.forward * Vector3d.Dot(Vessel.ReferenceTransform.forward, Vessel.srf_velocity.normalized);
                AoA =
                    Vector3d.Angle(AoAVec, vesselFacingAxis) * Math.Sign(Vector3d.Dot(AoAVec, Vessel.ReferenceTransform.forward));

                // Velocity vector projected onto the vehicle-horizontal plane
                Vector3d yawVec =
                    (Vector3d)vesselFacingAxis * Vector3d.Dot(vesselFacingAxis, Vessel.srf_velocity.normalized) +
                    (Vector3d)Vessel.ReferenceTransform.right * Vector3d.Dot(Vessel.ReferenceTransform.right, Vessel.srf_velocity.normalized);
                Yaw =
                    Vector3d.Angle(yawVec, vesselFacingAxis) * Math.Sign(Vector3d.Dot(yawVec, Vessel.ReferenceTransform.right));
            }
            else
                AoA = Yaw = 0;

            oldSpeed = Vessel.srfSpeed;
        }
    }
}
