﻿using Content.Shared.Alert;
 using Content.Shared.GameObjects.Components.Pulling;
 using Content.Shared.GameObjects.EntitySystems;
 using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Alert.Click
{
    /// <summary>
    /// Stop pulling something
    /// </summary>
    [UsedImplicitly]
    public class StopPulling : IAlertClick
    {
        void IExposeData.ExposeData(ObjectSerializer serializer) { }

        public void AlertClicked(ClickAlertEventArgs args)
        {
            EntitySystem
                .Get<SharedPullingSystem>()
                .GetPulled(args.Player)?
                .GetComponentOrNull<SharedPullableComponent>()?
                .TryStopPull();
        }
    }
}
