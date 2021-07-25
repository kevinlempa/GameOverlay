﻿// <copyright file="Buffs.cs" company="None">
// Copyright (c) None. All rights reserved.
// </copyright>

namespace GameHelper.RemoteObjects.Components
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using GameHelper.Utils;
    using GameOffsets.Objects.Components;
    using ImGuiNET;

    /// <summary>
    /// The <see cref="Buffs"/> component in the entity.
    /// </summary>
    public class Buffs : RemoteObjectBase
    {
        /// <summary>
        /// Stores Key to Effect mapping. This cache saves
        /// 2 x N x M read operations where:
        ///     N = total life components in gamehelper memory,
        ///     M = total number of buff those components has.
        /// </summary>
        private static ConcurrentDictionary<IntPtr, string> addressToEffectNameCache
            = new ConcurrentDictionary<IntPtr, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Buffs"/> class.
        /// </summary>
        /// <param name="address">address of the <see cref="Buffs"/> component.</param>
        public Buffs(IntPtr address)
            : base(address, true)
        {
        }

        /// <summary>
        /// Gets the Buffs/Debuffs associated with the entity.
        /// This is not updated anymore once entity dies.
        /// </summary>
        public ConcurrentDictionary<string, StatusEffectStruct> StatusEffects { get; private set; }
            = new ConcurrentDictionary<string, StatusEffectStruct>();

        /// <inheritdoc/>
        internal override void ToImGui()
        {
            base.ToImGui();
            if (ImGui.TreeNode("Status Effect (Buff/Debuff) (Click Effect to copy its name)"))
            {
                foreach (var kv in this.StatusEffects)
                {
                    UiHelper.DisplayTextAndCopyOnClick(
                        $"Name: {kv.Key} Details: {kv.Value}", kv.Key);
                }

                ImGui.TreePop();
            }
        }

        /// <inheritdoc/>
        protected override void CleanUpData()
        {
            throw new Exception("Component Address should never be Zero.");
        }

        /// <inheritdoc/>
        protected override void UpdateData(bool hasAddressChanged)
        {
            var reader = Core.Process.Handle;
            var data = reader.ReadMemory<BuffsOffsets>(this.Address);
            this.StatusEffects.Clear();
            var statusEffects = reader.ReadStdVector<IntPtr>(data.StatusEffectPtr);
            for (int i = 0; i < statusEffects.Length; i++)
            {
                var statusEffectData = reader.ReadMemory<StatusEffectStruct>(statusEffects[i]);
                if (addressToEffectNameCache.TryGetValue(statusEffectData.BuffDefinationPtr, out var oldEffectname))
                {
                    // existing Effect
                    this.StatusEffects[oldEffectname] = statusEffectData;
                }
                else if (this.TryGetNameFromBuffDefination(
                    statusEffectData.BuffDefinationPtr,
                    out var newEffectName))
                {
                    // New Effect.
                    this.StatusEffects[newEffectName] = statusEffectData;
                    addressToEffectNameCache[statusEffectData.BuffDefinationPtr] = newEffectName;
                }
            }
        }

        private bool TryGetNameFromBuffDefination(IntPtr addr, out string name)
        {
            var reader = Core.Process.Handle;
            var namePtr = reader.ReadMemory<IntPtr>(addr);
            name = reader.ReadUnicodeString(namePtr);
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return true;
        }
    }
}