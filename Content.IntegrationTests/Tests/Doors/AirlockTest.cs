﻿using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Doors;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using static Content.Server.GameObjects.Components.Doors.ServerDoorComponent;

namespace Content.IntegrationTests.Tests.Doors
{
    [TestFixture]
    [TestOf(typeof(AirlockComponent))]
    public class AirlockTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  name: PhysicsDummy
  id: PhysicsDummy
  components:
  - type: Physics
    anchored: false
    shapes:
    - !type:PhysShapeAabb
      bounds: ""-0.49,-0.49,0.49,0.49""
      layer:
      - Impassable

- type: entity
  name: AirlockDummy
  id: AirlockDummy
  components:
  - type: Airlock
  - type: Physics
    shapes:
    - !type:PhysShapeAabb
      bounds: ""-0.49,-0.49,0.49,0.49""
      mask:
      - Impassable
";
        [Test]
        public async Task OpenCloseDestroyTest()
        {
            var options = new ServerIntegrationOptions {ExtraPrototypes = Prototypes};
            var server = StartServerDummyTicker(options);

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();

            IEntity airlock = null;
            AirlockComponent airlockComponent = null;

            server.Assert(() =>
            {
                mapManager.CreateNewMapEntity(MapId.Nullspace);

                airlock = entityManager.SpawnEntity("AirlockDummy", MapCoordinates.Nullspace);

                Assert.True(airlock.TryGetComponent(out airlockComponent));
                Assert.That(airlockComponent.State, Is.EqualTo(DoorState.Closed));
            });

            await server.WaitIdleAsync();

            server.Assert(() =>
            {
                airlockComponent.Open();
                Assert.That(airlockComponent.State, Is.EqualTo(DoorState.Opening));
            });

            await server.WaitIdleAsync();

            await WaitUntil(server, () => airlockComponent.State == DoorState.Open);

            Assert.That(airlockComponent.State, Is.EqualTo(DoorState.Open));

            server.Assert(() =>
            {
                airlockComponent.Close();
                Assert.That(airlockComponent.State, Is.EqualTo(DoorState.Closing));
            });

            await WaitUntil(server, () => airlockComponent.State == DoorState.Closed);

            Assert.That(airlockComponent.State, Is.EqualTo(DoorState.Closed));

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    airlock.Delete();
                });
            });

            server.RunTicks(5);

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task AirlockBlockTest()
        {
            var options = new ServerIntegrationOptions {ExtraPrototypes = Prototypes};
            var server = StartServer(options);

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();

            IEntity physicsDummy = null;
            IEntity airlock = null;
            TestController controller = null;
            AirlockComponent airlockComponent = null;

            var physicsDummyStartingX = -1;

            server.Assert(() =>
            {
                var mapId = new MapId(1);
                mapManager.CreateNewMapEntity(mapId);

                var humanCoordinates = new MapCoordinates((physicsDummyStartingX, 0), mapId);
                physicsDummy = entityManager.SpawnEntity("PhysicsDummy", humanCoordinates);

                airlock = entityManager.SpawnEntity("AirlockDummy", new MapCoordinates((0, 0), mapId));

                Assert.True(physicsDummy.TryGetComponent(out IPhysicsComponent physics));

                controller = physics.EnsureController<TestController>();

                Assert.True(airlock.TryGetComponent(out airlockComponent));
                Assert.That(airlockComponent.State, Is.EqualTo(DoorState.Closed));
            });

            await server.WaitIdleAsync();

            // Push the human towards the airlock
            controller.LinearVelocity = (0.5f, 0);

            for (var i = 0; i < 240; i += 10)
            {
                // Keep the airlock awake so they collide
                airlock.GetComponent<IPhysicsComponent>().WakeBody();

                // Ensure that it is still closed
                Assert.That(airlockComponent.State, Is.EqualTo(DoorState.Closed));

                await server.WaitRunTicks(10);
                await server.WaitIdleAsync();
            }

            // Sanity check
            Assert.That(physicsDummy.Transform.MapPosition.X, Is.GreaterThan(physicsDummyStartingX));

            // Blocked by the airlock
            Assert.That(physicsDummy.Transform.MapPosition.X, Is.Negative.Or.Zero);
        }

        private class TestController : VirtualController { }
    }
}
