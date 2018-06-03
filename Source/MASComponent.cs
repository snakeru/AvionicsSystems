﻿/*****************************************************************************
 * The MIT License (MIT)
 * 
 * Copyright (c) 2016-2018 MOARdV
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to
 * deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 * 
 ****************************************************************************/
using System;
using System.Collections.Generic;
using System.Text;

namespace AvionicsSystems
{
    /// <summary>
    /// MASComponent encapsulates the action(s) attached to a particular prop.
    /// It is intended to contain multiple sub-nodes, each of which describe an
    /// action and the parameters required to trigger that action.  Note that
    /// some actions are considered one-shot, meaning they are triggered at
    /// Start() and the node isn't referenced again.
    /// </summary>
    public class MASComponent : InternalModule
    {
        [KSPField]
        public string startupScript;

        private List<IMASSubComponent> actions = new List<IMASSubComponent>();

        /// <summary>
        /// Create an IASAction-based object from a ConfigNode
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private static IMASSubComponent CreateAction(ConfigNode config, InternalProp prop, MASFlightComputer comp)
        {
            switch (config.name)
            {
                case "ANIMATION":
                    return new MASActionAnimation(config, prop, comp);
                case "ANIMATION_PLAYER":
                    return new MASActionAnimationPlayer(config, prop, comp);
                case "AUDIO_PLAYER":
                    return new MASActionAudioPlayer(config, prop, comp);
                case "COLOR_SHIFT":
                    return new MASActionColorShift(config, prop, comp);
                case "COLLIDER_EVENT":
                    return new MASActionColliderEvent(config, prop, comp);
                case "IMAGE":
                    return new MASActionImage(config, prop, comp);
                case "INT_LIGHT":
                    return new MASActionIntLight(config, prop, comp);
                case "MODEL_SCALE":
                    return new MASActionModelScale(config, prop, comp);
                case "ROTATION":
                    return new MASActionRotation(config, prop, comp);
                case "TEXT_LABEL":
                    return new MASActionTextLabel(config, prop, comp);
                case "TEXTURE_SHIFT":
                    return new MASActionTextureShift(config, prop, comp);
                case "TRANSLATION":
                    return new MASActionTranslation(config, prop, comp);
                case "TRIGGER_EVENT":
                    return new MASActionTriggerEvent(config, prop, comp);
                default:
                    Utility.LogError(config, "Unrecognized MASComponent child node {0} found", config.name);
                    return null;
            }
        }

        /// <summary>
        /// Configure this module and its children.
        /// </summary>
        public void Start()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                return;
            }

            try
            {
                MASFlightComputer comp = MASFlightComputer.Instance(internalProp.part);
                if (comp == null)
                {
                    throw new ArgumentNullException("Unable to find MASFlightComputer in part - please check part configs");
                }

                ConfigNode moduleConfig = Utility.GetPropModuleConfigNode(internalProp.propName, ClassName);
                if (moduleConfig == null)
                {
                    throw new ArgumentNullException("No ConfigNode found!");
                }

                int nodeCount = 0;
                ConfigNode[] actionNodes = moduleConfig.GetNodes();
                for (int i = 0; i < actionNodes.Length; ++i)
                {
                    try
                    {
                        IMASSubComponent action = CreateAction(actionNodes[i], internalProp, comp);
                        if (action != null)
                        {
                            ++nodeCount;
                            actions.Add(action);
                        }
                    }
                    catch (Exception e)
                    {
                        string componentName = string.Empty;
                        if (!actionNodes[i].TryGetValue("name", ref componentName))
                        {
                            componentName = "anonymous";
                        }

                        string message = string.Format("Error configuring prop #{0} ({1})", internalProp.propID, internalProp.propName);
                        Utility.LogError(this, message);
                        Utility.LogError(this, "Error in " + actionNodes[i].name + " " + componentName + ":");
                        Utility.LogError(this, "{0}", e.ToString());
                        Utility.ComplainLoudly(message);
                    }
                }

                // If an initialization script was supplied, call it.
                if (!string.IsNullOrEmpty(startupScript))
                {
                    Action startup = comp.GetAction(startupScript, internalProp);
                    startup();
                }

                Utility.LogMessage(this, "Configuration complete in prop #{0} ({1}): {2} nodes created", internalProp.propID, internalProp.propName, nodeCount);
            }
            catch (Exception e)
            {
                string message = string.Format("Failed to configure prop #{0} ({1})", internalProp.propID, internalProp.propName);
                Utility.ComplainLoudly(message);
                Utility.LogError(this, message);
                Utility.LogError(this, e.ToString());
            }
        }

        /// <summary>
        /// Release resources used by this module's children.
        /// </summary>
        public void OnDestroy()
        {
            try
            {
                MASFlightComputer comp = MASFlightComputer.Instance(internalProp.part);

                for (int i = 0; i < actions.Count; ++i)
                {
                    actions[i].ReleaseResources(comp, internalProp);
                }
            }
            catch { }
        }

        /// <summary>
        /// Call the startupScript, if it exists.
        /// </summary>
        /// <param name="comp">The MASFlightComputer for this prop.</param>
        /// <returns>true if a script exists, false otherwise.</returns>
        internal bool RunStartupScript(MASFlightComputer comp)
        {
            if (!string.IsNullOrEmpty(startupScript))
            {
                Action startup = comp.GetAction(startupScript, internalProp);
                startup();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
