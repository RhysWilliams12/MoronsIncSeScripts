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
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.
        // 
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.

        private const string Miner = "Miner";
        private const string Hinge = "Hinge";
        private const string Reverse = "Reverse";

        private const float BlockWidth = 2.5f;
        private const float PistonVelocity = 0.5f;
        private const float DesiredWorkingArcLengthPerSecond = 0.25f;

        List<IMyBlockGroup> minerGroups = new List<IMyBlockGroup>();

        public Program()
        {
            Echo("Starting hinge miner script.");
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            GridTerminalSystem.GetBlockGroups(minerGroups, group => {
                string name = group.Name.ToLower();
                return name.Contains(Miner) && name.Contains(Hinge);
            });

            Echo($"Found {minerGroups.Count} hinge miners.");

            minerGroups.ForEach(group =>
            {
                List<IMyFunctionalBlock> blocks = new List<IMyFunctionalBlock>();
                group.GetBlocksOfType(blocks);
                blocks.ForEach(EnableBlock);
            });
        }

        public void Main(string argument, UpdateType updateSource)
        {
            minerGroups.ForEach(RunMiner);
        }

        private void RunMiner(IMyBlockGroup miner)
        {
            List<IMyExtendedPistonBase> pistons = new List<IMyExtendedPistonBase>();
            List<IMyMotorAdvancedStator> hinges = new List<IMyMotorAdvancedStator>();
            List<IMyShipDrill> drills = new List<IMyShipDrill>();

            miner.GetBlocksOfType(pistons);
            miner.GetBlocksOfType(hinges);

            pistons.ForEach(piston =>
            {
                piston.MaxLimit = 0;
                piston.Velocity = PistonVelocity / pistons.Count;
            });
            FindHorizontalHinge(hinges, drills);

            IMyMotorAdvancedStator hinge = hinges.Last();
            hinge.TargetVelocityRad = 1 / (CalculateCircumference(pistons) / DesiredWorkingArcLengthPerSecond);

            float hingeAngle = MathHelper.RoundOn2(MathHelper.ToDegrees(hinge.Angle));
            if (hingeAngle <= MathHelper.RoundOn2(hinge.LowerLimitDeg) || hingeAngle >= MathHelper.RoundOn2(hinge.UpperLimitDeg))
            {
                IncrementMiner(hinge, pistons);
            }

        }

        private IMyMotorAdvancedStator FindHorizontalHinge(List<IMyMotorAdvancedStator> hinges, List<IMyShipDrill> drills)
        {
            IMyShipDrill drill = drills.FirstOrDefault();
            return hinges.FirstOrDefault(hinge =>
            {
                Vector3D hingePosition = hinge.GetPosition();
                Vector3D drillPosition = drill.GetPosition();
                Echo($"Hinge {hingePosition.X},{hingePosition.Y},{hingePosition.Z} : Drill {drillPosition.X},{drillPosition.Y},{drillPosition.Z}");
                if (hingePosition.X - drillPosition.X <= 1 || hingePosition.X + drillPosition.X <= 1)
                {

                }
                return true;
            });
        }

        private float CalculateCircumference(List<IMyExtendedPistonBase> pistons)
        {
            float pistonsBaseLength = pistons.Count * BlockWidth * 2;
            float drillLength = BlockWidth * 3;
            float hingeLength = BlockWidth;
            return MathHelper.TwoPi * (pistonsBaseLength + pistons.Sum(piston => piston.CurrentPosition) + drillLength + hingeLength);
        }

        private void IncrementMiner(IMyMotorAdvancedStator hinge, List<IMyExtendedPistonBase> pistons)
        {
            hinge.ApplyAction(Reverse);
            float incrementLength = BlockWidth / 2;
            pistons.ForEach(piston => piston.MaxLimit += incrementLength);
        }

        private void EnableBlock<T>(T block) where T: IMyFunctionalBlock
        {
            block.Enabled = true;
        }
    }
}
