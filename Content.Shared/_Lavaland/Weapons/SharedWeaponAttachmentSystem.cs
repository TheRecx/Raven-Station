// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aineias1 <dmitri.s.kiselev@gmail.com>
// SPDX-FileCopyrightText: 2025 FaDeOkno <143940725+FaDeOkno@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 McBosserson <148172569+McBosserson@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Milon <plmilonpl@gmail.com>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 Rouden <149893554+Roudenn@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Ted Lukin <66275205+pheenty@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 TheBorzoiMustConsume <197824988+TheBorzoiMustConsume@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Unlumination <144041835+Unlumy@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
// SPDX-FileCopyrightText: 2025 username <113782077+whateverusername0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 whateverusername0 <whateveremail>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Hands.Components;
using Content.Shared.Light.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lavaland.Weapons;

public abstract partial class SharedWeaponAttachmentSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeaponAttachmentComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WeaponAttachmentComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<WeaponAttachmentComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<WeaponAttachmentComponent, EntInsertedIntoContainerMessage>(OnEntInsertedIntoContainer);
        SubscribeLocalEvent<WeaponAttachmentComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromContainer);
    }

    private void OnMapInit(EntityUid uid, WeaponAttachmentComponent component, MapInitEvent args)
    {
        var itemSlots = EnsureComp<ItemSlotsComponent>(uid);
        var bayonetSlot = new ItemSlot
        {
            Whitelist = new EntityWhitelist { Components = ["AttachmentBayonet"] },
            Swap = false,
            EjectOnBreak = true,
            Name = Loc.GetString("attachment-bayonet-slot-name")
        };
        var lightSlot = new ItemSlot
        {
            Whitelist = new EntityWhitelist { Components = ["AttachmentFlashlight"] },
            Swap = false,
            EjectOnBreak = true,
            Name = Loc.GetString("attachment-light-slot-name"),
            OccludesLight = false,
        };
        _itemSlots.AddItemSlot(uid, WeaponAttachmentComponent.BayonetSlotId, bayonetSlot, itemSlots);
        _itemSlots.AddItemSlot(uid, WeaponAttachmentComponent.LightSlotId, lightSlot, itemSlots);
    }

    private void OnShutdown(EntityUid uid, WeaponAttachmentComponent component, ComponentShutdown args)
    {
        RemoveToggleAction(component);
    }

    private void OnGetActions(EntityUid uid, WeaponAttachmentComponent component, GetItemActionsEvent args)
    {
        if (component.LightAttached && component.ToggleLightAction != null)
            args.AddAction(ref component.ToggleLightAction, component.LightActionPrototype);
    }

    private void CreateToggleAction(EntityUid uid, WeaponAttachmentComponent component)
    {
        if (component.ToggleLightAction != null)
            return;

        _actions.AddAction(uid, ref component.ToggleLightAction, component.LightActionPrototype);
    }

    private void RemoveToggleAction(WeaponAttachmentComponent component)
    {
        if (component.ToggleLightAction == null)
            return;

        _actions.RemoveAction(component.ToggleLightAction.Value);
        component.ToggleLightAction = null;
    }

    private void OnEntInsertedIntoContainer(EntityUid uid, WeaponAttachmentComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == WeaponAttachmentComponent.BayonetSlotId
            && HasComp<AttachmentBayonetComponent>(args.Entity))
            BayonetChanged(uid, true, component);
        else if (args.Container.ID == WeaponAttachmentComponent.LightSlotId
            && HasComp<AttachmentFlashlightComponent>(args.Entity))
            AttachLight(uid, args.Entity, component);
    }

    private void OnEntRemovedFromContainer(EntityUid uid, WeaponAttachmentComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == WeaponAttachmentComponent.BayonetSlotId
            && HasComp<AttachmentBayonetComponent>(args.Entity))
            BayonetChanged(uid, false, component);
        else if (args.Container.ID == WeaponAttachmentComponent.LightSlotId
            && HasComp<AttachmentFlashlightComponent>(args.Entity))
            RemoveLight(uid, component);
    }

    private void BayonetChanged(EntityUid uid, bool attached, WeaponAttachmentComponent component)
    {
        if (component.BayonetAttached == attached
            || !TryComp<MeleeWeaponComponent>(uid, out var meleeComp))
            return;

        component.BayonetAttached = attached;

        if (attached)
        {
            meleeComp.Damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Slash"), 12);
            meleeComp.AttackRate = 1.5f;
            meleeComp.HitSound = new SoundPathSpecifier("/Audio/Weapons/bladeslice.ogg");
            AddSharp(uid);
        }
        else
        {
            meleeComp.Damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), 5);
            meleeComp.AttackRate = 1f;
            meleeComp.HitSound = null;
            RemSharp(uid);
        }

        Dirty(uid, component);
    }

    // Due to SharpComponent not being shared, we need to override this in the server.
    protected abstract void AddSharp(EntityUid uid);
    protected abstract void RemSharp(EntityUid uid);

    private void AttachLight(EntityUid uid, EntityUid light, WeaponAttachmentComponent component)
    {
        if (component.LightAttached)
            return;

        component.LightAttached = true;
        if (TryComp<HandheldLightComponent>(light, out var lightComp))
            component.LightOn = lightComp.Activated;

        CreateToggleAction(uid, component);

        // Manually trigger a refresh in case the entity is being held by a player.
        if (TryComp<HandsComponent>(Transform(uid).ParentUid, out var hands))
        {
            var ev = new GetItemActionsEvent(_actionContainer, Transform(uid).ParentUid, uid);
            RaiseLocalEvent(uid, ev);
        }

        Dirty(uid, component);
    }

    private void RemoveLight(EntityUid uid, WeaponAttachmentComponent component)
    {
        if (!component.LightAttached)
            return;

        component.LightAttached = false;
        component.LightOn = false;
        RemoveToggleAction(component);
        Dirty(uid, component);
    }
}