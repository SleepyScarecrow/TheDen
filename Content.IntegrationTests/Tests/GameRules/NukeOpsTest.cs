// SPDX-FileCopyrightText: 2024 DEATHB4DEFEAT <77995199+DEATHB4DEFEAT@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Errant <35878406+Errant-4@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 VMSolidus <evilexecutive@gmail.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

/* WD edit

#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.Body.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.NPC.Systems;
using Content.Server.Pinpointer;
using Content.Server.Roles;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.NPC.Systems;
using Content.Shared.NukeOps;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.IntegrationTests.Tests.GameRules;

[TestFixture]
public sealed class NukeOpsTest
{
    /// <summary>
    /// Check that a nuke ops game mode can start without issue. I.e., that the nuke station and such all get loaded.
    /// </summary>
    [Test]
    public async Task TryStopNukeOpsFromConstantlyFailing()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Dirty = true,
            DummyTicker = false,
            Connected = true,
            InLobby = true
        });

        var server = pair.Server;
        var client = pair.Client;
        var entMan = server.EntMan;
        var mapSys = server.System<MapSystem>();
        var ticker = server.System<GameTicker>();
        var mindSys = server.System<MindSystem>();
        var roleSys = server.System<RoleSystem>();
        var invSys = server.System<InventorySystem>();
        var factionSys = server.System<NpcFactionSystem>();

        server.CfgMan.SetCVar(CCVars.GridFill, true);

        // Initially in the lobby
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        Assert.That(client.AttachedEntity, Is.Null);
        Assert.That(ticker.PlayerGameStatuses[client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));

        // Opt into the nukies role.
        await pair.SetAntagPref("NukeopsCommander", true);

        // There are no grids or maps
        Assert.That(entMan.Count<MapComponent>(), Is.Zero);
        Assert.That(entMan.Count<MapGridComponent>(), Is.Zero);
        Assert.That(entMan.Count<StationMapComponent>(), Is.Zero);
        Assert.That(entMan.Count<StationMemberComponent>(), Is.Zero);
        Assert.That(entMan.Count<StationCentcommComponent>(), Is.Zero);

        // And no nukie related components
        Assert.That(entMan.Count<NukeopsRuleComponent>(), Is.Zero);
        Assert.That(entMan.Count<NukeopsRoleComponent>(), Is.Zero);
        Assert.That(entMan.Count<NukeOperativeComponent>(), Is.Zero);
        Assert.That(entMan.Count<NukeOpsShuttleComponent>(), Is.Zero);
        Assert.That(entMan.Count<NukeOperativeSpawnerComponent>(), Is.Zero);

        // Ready up and start nukeops
        await pair.WaitClientCommand("toggleready True");
        Assert.That(ticker.PlayerGameStatuses[client.User!.Value], Is.EqualTo(PlayerGameStatus.ReadyToPlay));
        await pair.WaitCommand("forcepreset Nukeops");
        await pair.RunTicksSync(10);

        // Game should have started
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        Assert.That(ticker.PlayerGameStatuses[client.User!.Value], Is.EqualTo(PlayerGameStatus.JoinedGame));
        Assert.That(client.EntMan.EntityExists(client.AttachedEntity));
        var player = pair.Player!.AttachedEntity!.Value;
        Assert.That(entMan.EntityExists(player));

        // Maps now exist
        Assert.That(entMan.Count<MapComponent>(), Is.GreaterThan(0));
        Assert.That(entMan.Count<MapGridComponent>(), Is.GreaterThan(0));
        Assert.That(entMan.Count<StationDataComponent>(), Is.EqualTo(2)); // The main station & nukie station
        Assert.That(entMan.Count<StationMemberComponent>(), Is.GreaterThan(3)); // Each station has at least 1 grid, plus some shuttles
        Assert.That(entMan.Count<StationCentcommComponent>(), Is.EqualTo(1));

        // And we now have nukie related components
        Assert.That(entMan.Count<NukeopsRuleComponent>(), Is.EqualTo(1));
        Assert.That(entMan.Count<NukeopsRoleComponent>(), Is.EqualTo(1));
        Assert.That(entMan.Count<NukeOperativeComponent>(), Is.EqualTo(1));
        Assert.That(entMan.Count<NukeOpsShuttleComponent>(), Is.EqualTo(1));

        // The player entity should be the nukie commander
        var mind = mindSys.GetMind(player)!.Value;
        Assert.That(entMan.HasComponent<NukeOperativeComponent>(player));
        Assert.That(roleSys.MindIsAntagonist(mind));
        Assert.That(roleSys.MindHasRole<NukeopsRoleComponent>(mind));

        Assert.That(factionSys.IsMember(player, "Syndicate"), Is.True);
        Assert.That(factionSys.IsMember(player, "NanoTrasen"), Is.False);
        var roles = roleSys.MindGetAllRoleInfo(mind);
        var cmdRoles = roles.Where(x => x.Prototype == "NukeopsCommander");
        Assert.That(cmdRoles.Count(), Is.EqualTo(1));

        // The second dummy player should be a medic
        var dummyMind = mindSys.GetMind(dummyEnts[1])!.Value;
        Assert.That(entMan.HasComponent<NukeOperativeComponent>(dummyEnts[1]));
        Assert.That(roleSys.MindIsAntagonist(dummyMind));
        Assert.That(roleSys.MindHasRole<NukeopsRoleComponent>(dummyMind));
        Assert.That(factionSys.IsMember(dummyEnts[1], "Syndicate"), Is.True);
        Assert.That(factionSys.IsMember(dummyEnts[1], "NanoTrasen"), Is.False);
        roles = roleSys.MindGetAllRoleInfo(dummyMind);
        cmdRoles = roles.Where(x => x.Prototype == "NukeopsMedic");
        Assert.That(cmdRoles.Count(), Is.EqualTo(1));

        // The other two players should have just spawned in as normal.
        CheckDummy(0);
        CheckDummy(2);
        void CheckDummy(int i)
        {
            var ent = dummyEnts[i];
            var mindCrew = mindSys.GetMind(ent)!.Value;
            Assert.That(entMan.HasComponent<NukeOperativeComponent>(ent), Is.False);
            Assert.That(roleSys.MindIsAntagonist(mindCrew), Is.False);
            Assert.That(roleSys.MindHasRole<NukeopsRoleComponent>(mindCrew), Is.False);
            Assert.That(factionSys.IsMember(ent, "Syndicate"), Is.False);
            Assert.That(factionSys.IsMember(ent, "NanoTrasen"), Is.True);
            var nukeroles = new List<string>() { "Nukeops", "NukeopsMedic", "NukeopsCommander" };
            Assert.That(roleSys.MindGetAllRoleInfo(mindCrew).Any(x => nukeroles.Contains(x.Prototype)), Is.False);
        }

        // The game rule exists, and all the stations/shuttles/maps are properly initialized
        var rule = entMan.AllComponents<NukeopsRuleComponent>().Single().Component;
        var gridsRule = entMan.AllComponents<RuleGridsComponent>().Single().Component;
        foreach (var grid in gridsRule.MapGrids)
        {
            Assert.That(entMan.EntityExists(grid));
            Assert.That(entMan.HasComponent<MapGridComponent>(grid));
            Assert.That(entMan.HasComponent<StationMemberComponent>(grid));
        }
        Assert.That(entMan.EntityExists(rule.NukieOutpost));
        Assert.That(entMan.EntityExists(rule.NukieShuttle));
        Assert.That(entMan.EntityExists(rule.TargetStation));

        Assert.That(entMan.HasComponent<MapGridComponent>(rule.NukieOutpost));
        Assert.That(entMan.HasComponent<MapGridComponent>(rule.NukieShuttle));

        Assert.That(entMan.HasComponent<StationMemberComponent>(rule.NukieOutpost));
        Assert.That(entMan.HasComponent<StationDataComponent>(rule.TargetStation));

        var nukieShuttlEnt = entMan.AllComponents<NukeOpsShuttleComponent>().FirstOrDefault().Uid;
        Assert.That(entMan.EntityExists(nukieShuttlEnt));

        EntityUid? nukieStationEnt = null;
        foreach (var grid in gridsRule.MapGrids)
        {
            if (entMan.HasComponent<StationMemberComponent>(grid))
            {
                nukieStationEnt = grid;
                break;
            }
        }

        Assert.That(entMan.EntityExists(nukieStationEnt));
        var nukieStation = entMan.GetComponent<StationMemberComponent>(nukieStationEnt!.Value);

        var nukieStation = entMan.GetComponent<StationMemberComponent>(rule.NukieOutpost!.Value);
        Assert.That(entMan.EntityExists(nukieStation.Station));
        Assert.That(nukieStation.Station, Is.Not.EqualTo(rule.TargetStation));

        Assert.That(server.MapMan.MapExists(gridsRule.Map));
        var nukieMap = mapSys.GetMap(gridsRule.Map!.Value);

        var targetStation = entMan.GetComponent<StationDataComponent>(rule.TargetStation!.Value);
        var targetGrid = targetStation.Grids.First();
        var targetMap = entMan.GetComponent<TransformComponent>(targetGrid).MapUid!.Value;
        Assert.That(targetMap, Is.Not.EqualTo(nukieMap));

        Assert.That(entMan.GetComponent<TransformComponent>(player).MapUid, Is.EqualTo(nukieMap));
        Assert.That(entMan.GetComponent<TransformComponent>(nukieStationEnt.Value).MapUid, Is.EqualTo(nukieMap));
        Assert.That(entMan.GetComponent<TransformComponent>(nukieShuttlEnt).MapUid, Is.EqualTo(nukieMap));
        Assert.That(entMan.GetComponent<TransformComponent>(rule.NukieOutpost!.Value).MapUid, Is.EqualTo(nukieMap));
        Assert.That(entMan.GetComponent<TransformComponent>(rule.NukieShuttle!.Value).MapUid, Is.EqualTo(nukieMap));

        // The maps are all map-initialized, including the player
        // Yes, this is necessary as this has repeatedly been broken somehow.
        Assert.That(mapSys.IsInitialized(nukieMap));
        Assert.That(mapSys.IsInitialized(targetMap));
        Assert.That(mapSys.IsPaused(nukieMap), Is.False);
        Assert.That(mapSys.IsPaused(targetMap), Is.False);

        EntityLifeStage LifeStage(EntityUid? uid) => entMan.GetComponent<MetaDataComponent>(uid!.Value).EntityLifeStage;
        Assert.That(LifeStage(player), Is.GreaterThan(EntityLifeStage.Initialized));
        Assert.That(LifeStage(nukieMap), Is.GreaterThan(EntityLifeStage.Initialized));
        Assert.That(LifeStage(targetMap), Is.GreaterThan(EntityLifeStage.Initialized));
        Assert.That(LifeStage(nukieStationEnt.Value), Is.GreaterThan(EntityLifeStage.Initialized));
        Assert.That(LifeStage(nukieShuttlEnt), Is.GreaterThan(EntityLifeStage.Initialized));
        Assert.That(LifeStage(rule.NukieOutpost), Is.GreaterThan(EntityLifeStage.Initialized));
        Assert.That(LifeStage(rule.NukieShuttle), Is.GreaterThan(EntityLifeStage.Initialized));
        Assert.That(LifeStage(rule.TargetStation), Is.GreaterThan(EntityLifeStage.Initialized));

        // Make sure the player has hands. We've had fucking disarmed nukies before.
        Assert.That(entMan.HasComponent<HandsComponent>(player));
        Assert.That(entMan.GetComponent<HandsComponent>(player).Hands.Count, Is.GreaterThan(0));

        // While we're at it, lets make sure they aren't naked. I don't know how many inventory slots all mobs will be
        // likely to have in the future. But nukies should probably have at least 3 slots with something in them.
        var enumerator = invSys.GetSlotEnumerator(player);
        int total = 0;
        while (enumerator.NextItem(out _))
        {
            total++;
        }
        Assert.That(total, Is.GreaterThan(3));

        // Finally lets check the nukie commander passed basic training and figured out how to breathe.
        var totalSeconds = 30;
        var totalTicks = (int) Math.Ceiling(totalSeconds / server.Timing.TickPeriod.TotalSeconds);
        int increment = 5;
        var resp = entMan.GetComponent<RespiratorComponent>(player);
        var damage = entMan.GetComponent<DamageableComponent>(player);
        for (var tick = 0; tick < totalTicks; tick += increment)
        {
            await pair.RunTicksSync(increment);
            Assert.That(resp.SuffocationCycWles, Is.LessThanOrEqualTo(resp.SuffocationCycleThreshold));
            Assert.That(damage.TotalDamage, Is.EqualTo(FixedPoint2.Zero));
        }

        // Check that the round does not end prematurely when agents are deleted in the outpost
        var nukies = dummyEnts.Where(entMan.HasComponent<NukeOperativeComponent>).Append(player).ToArray();
        await server.WaitAssertion(() =>
        {
            for (var i = 0; i < nukies.Length - 1; i++)
            {
                entMan.DeleteEntity(nukies[i]);
                Assert.That(roundEndSys.IsRoundEndRequested,
                    Is.False,
                    $"The round ended, but {nukies.Length - i - 1} nukies are still alive!");
            }
            // Delete the last nukie and make sure the round ends.
            entMan.DeleteEntity(nukies[^1]);

            Assert.That(roundEndSys.IsRoundEndRequested,
                "All nukies were deleted, but the round didn't end!");
        });

        ticker.SetGamePreset((GamePresetPrototype?) null);
        await pair.CleanReturnAsync();
    }
}

*/
