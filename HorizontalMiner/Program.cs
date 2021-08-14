using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private const string Miner = "miner";
        private const string Horizontal = "horizontal";

        private const float BlockWidth = 2.5f;
        private const float BreakingTorque = 33600000f;
        private const float PistonWorkingWidth = 2.5f;
        private const float DesiredWorkingArcPerSecond = 0.25f;
        private const float PistonVelocity = 0.1f;

        private readonly List<IMyBlockGroup> minerGroups = new List<IMyBlockGroup>();
        private readonly Dictionary<string, bool> minerState = new Dictionary<string, bool>();


        public Program()
        {
            Echo("Starting horizontal miner script.");
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            GridTerminalSystem.GetBlockGroups(minerGroups, group =>
            {
                string name = group.Name.ToLower();
                return name.Contains(Miner) && name.Contains(Horizontal);
            });
            Echo($"Found: {minerGroups.Count} horizontal miners.");

            List<IMyShipDrill> drills = new List<IMyShipDrill>();
            List<IMyMotorAdvancedStator> rotors = new List<IMyMotorAdvancedStator>();

            minerGroups.ForEach(group =>
            {
                group.GetBlocksOfType(drills);
                group.GetBlocksOfType(rotors);
            });

            drills.ForEach(drill => drill.Enabled = true);
            rotors.ForEach(rotor =>
            {
                rotor.BrakingTorque = BreakingTorque;
                rotor.Enabled = true;
            });
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Terminal)
            {
                IMyTextSurface surface = Me.GetSurface(Me.SurfaceCount - 1);
                surface.BackgroundColor = Color.Green;
                surface.FontSize = 24;
                surface.WriteText("Horizontal Miners");
            }
            minerGroups.ForEach(RunMiner);
        }

        private void RunMiner(IMyBlockGroup miner)
        {
            List<IMyShipDrill> drills = new List<IMyShipDrill>();
            List<IMyExtendedPistonBase> pistons = new List<IMyExtendedPistonBase>();
            List<IMyMotorAdvancedStator> rotors = new List<IMyMotorAdvancedStator>();

            miner.GetBlocksOfType(rotors);
            miner.GetBlocksOfType(pistons);

            bool hasFullyExtended = pistons.All(HasMinerFullyExtended);
            bool hasFinished = minerState.GetValueOrDefault(miner.Name);
            if (hasFinished || hasFullyExtended)
            {
                if (!minerState.Keys.Contains(miner.Name))
                {
                    minerState.Add(miner.Name, true);
                }

                Echo($"{miner.Name} has fininshed mining.");
                pistons.ForEach(piston => piston.Velocity = -PistonVelocity);
                rotors.ForEach(rotor => rotor.Enabled = false);
                drills.ForEach(drill => drill.Enabled = false);
            }
            else
            {
                IMyMotorAdvancedStator rotor = rotors.FirstOrDefault();
                if (rotor != null)
                {
                    rotor.Enabled = true;
                    float period = CalculateCircumference(pistons) / DesiredWorkingArcPerSecond;
                    rotor.TargetVelocityRad = 1 / period;
                    pistons.ForEach(piston => piston.Velocity = PistonWorkingWidth / (period * pistons.Count));

                    if (rotor.LowerLimitDeg != 0 || rotor.UpperLimitDeg != 0)
                    {
                        float rotorAngle = MathHelper.RoundOn2(MathHelper.ToDegrees(rotor.Angle));
                        if (rotorAngle <= MathHelper.RoundOn2(rotor.LowerLimitDeg) || rotorAngle >= MathHelper.RoundOn2(rotor.UpperLimitDeg))
                        {
                            rotor.TargetVelocityRad = -rotor.TargetVelocityRad;
                        }
                    }
                }
                else
                {
                    pistons.ForEach(piston => piston.Velocity = PistonVelocity / pistons.Count);
                }
            }
        }

        private bool HasMinerFullyExtended(IMyExtendedPistonBase piston)
        {
            return MathHelper.RoundOn2(piston.CurrentPosition) == MathHelper.RoundOn2(piston.MaxLimit);
        }

        private float CalculateCircumference(List<IMyExtendedPistonBase> pistons)
        {
            float drillWidth = BlockWidth * 3;
            float rotorWidth = BlockWidth / 2;
            return MathHelper.TwoPi * (pistons.Sum(piston => piston.CurrentPosition) + (pistons.Count * BlockWidth * 2) + drillWidth + rotorWidth);
        }

    }
}
