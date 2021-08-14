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
        private const string Vertical = "vertical";

        private const float BlockWidth = 2.5f;
        private const float PistonWorkingWidth = 2.5f;
        private const float DesiredWorkingArcPerSecond = 0.5f;
        private const float PistonVelocity = 0.1f;

        private readonly List<IMyBlockGroup> minerGroups = new List<IMyBlockGroup>();
        private readonly Dictionary<string, bool> minerState = new Dictionary<string, bool>();
        public Program()
        {
            Echo("Starting vertical miner script.");
            GridTerminalSystem.GetBlockGroups(minerGroups, (group) =>
             {
                 string name = group.Name.ToLower();
                 return name.Contains(Miner) && name.Contains(Vertical);
             });
            Echo($"Found: {minerGroups.Count} vertical miners.");

            List<IMyShipDrill> drills = new List<IMyShipDrill>();

            minerGroups.ForEach(group =>
            {
                group.GetBlocksOfType(drills);
            });

            drills.ForEach(drill => drill.Enabled = true);

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Terminal)
            {
                IMyTextSurface surface = Me.GetSurface(Me.SurfaceCount - 1);
                surface.BackgroundColor = Color.Green;
                surface.FontSize = 24;
                surface.WriteText("Vertical Miners");
            }
            minerGroups.ForEach(RunMiner);
        }

        private void RunMiner(IMyBlockGroup miner)
        {
            List<IMyExtendedPistonBase> pistons = new List<IMyExtendedPistonBase>();
            List<IMyMotorAdvancedStator> rotors = new List<IMyMotorAdvancedStator>();
            List<IMyShipDrill> drills = new List<IMyShipDrill>();

            miner.GetBlocksOfType(rotors);
            miner.GetBlocksOfType(pistons);
            miner.GetBlocksOfType(drills);

            bool fullyExtended = pistons.All(HasMinerFullyExtended);
            bool finished = minerState.GetValueOrDefault(miner.Name);
            if (finished || fullyExtended)
            {
                minerState.Add(miner.Name, true);

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
                    float period = MathHelper.TwoPi * (drills.Count * BlockWidth) / DesiredWorkingArcPerSecond;
                    rotor.TargetVelocityRad = 1 / period;
                    pistons.ForEach(piston => piston.Velocity = PistonWorkingWidth / (period * pistons.Count));
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
    }
}
