using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using static CorewoodFortified.PieceAssets;

namespace CorewoodFortified;

internal class AddFortified
{
    public static void LoadPieces()
    {
        PieceAssets.Init();

        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedEntrance);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedEntranceDrawbridge);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedWall);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedWallStairs);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedWallStairsAlt);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedTjunction);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedCorner);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedCornerAlt);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedCornerCurved);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedWallElevator);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedWatchtower);

        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedEntranceRoofed);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedEntranceDrawbridgeRoofed);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedWallRoofed);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedWallStairsRoofed);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedWallStairsAltRoofed);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedTjunctionRoofed);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedCornerRoofed);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedCornerAltRoofed);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedCornerCurvedRoofed);

        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedGates);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedDrawbridge);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedLiftGate);

        CorewoodFortifiedMod.TryAddPiece(AddPieceInternalWall);
        CorewoodFortifiedMod.TryAddPiece(AddPieceInternalDoorway);
        CorewoodFortifiedMod.TryAddPiece(AddPieceInternalCorner);
        CorewoodFortifiedMod.TryAddPiece(AddPieceInternalCurvedCorner);
        CorewoodFortifiedMod.TryAddPiece(AddPieceInternalTjunction);

        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedFence);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedFenceLarge);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedFenceWindowed);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedFenceGate);

        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedSpikes);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedSpikesBronze);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedSpikesIron);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedSpikesBlackMetal);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedSpikesFlametal);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedSpikesBloodgold);

        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedBrazierSmall);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedBrazierLarge);

        CorewoodFortifiedMod.TryAddPiece(ChainTowerPieces.AddPieceChainTowerSmall);
        CorewoodFortifiedMod.TryAddPiece(ChainTowerPieces.AddPieceChainTowerMedium);
        CorewoodFortifiedMod.TryAddPiece(ChainTowerPieces.AddPieceChainTowerLarge);

        CorewoodFortifiedMod.TryAddPiece(ZiplineSetup.AddPieceZiplinePost);

        CorewoodFortifiedMod.TryAddPiece(AddPieceFortifiedBallista);

        CorewoodFortifiedMod.TryAddPiece(AddPieceFlameTrap);
        CorewoodFortifiedMod.TryAddPiece(AddPieceSawWall);
        CorewoodFortifiedMod.TryAddPiece(AddPieceSpikeFloor);
        CorewoodFortifiedMod.TryAddPiece(AddPieceTrapController);

        CorewoodFortifiedMod.TryAddPiece(AddPieceElectricFence);
        CorewoodFortifiedMod.TryAddPiece(AddPieceElectricFence4m);
        CorewoodFortifiedMod.TryAddPiece(AddPieceFenceController);
    }

    public static void AddPieceFortifiedEntrance()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_entrance");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 30,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 8,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedEntranceDrawbridge()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_drawbridge_large");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[3]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 30,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 8,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Chain",
                    Amount = 8,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        SetDoorEffects(gameObject, fortifiedOpen, fortifiedClose);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedWall(){
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_wall");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 40,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 8,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedWallStairs(){
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_wall_stairs");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 30,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 8,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedWallStairsAlt(){
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_wall_stairs_alt");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 30,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 8,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedTjunction()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_tjunction");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 40,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 12,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedCorner(){
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_corner");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 30,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 8,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedCornerAlt(){
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_corner_alt");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 30,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 8,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedCornerCurved()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_corner_curved");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 30,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 8,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedWallElevator()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_wall_elevator");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 35,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 12,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        ElevatorSetup.Prepare(gameObject);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedWatchtower()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_corner_tower");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "forge",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[3]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 60,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 20,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Iron",
                    Amount = 3,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        ElevatorSetup.Prepare(gameObject);
        PieceManager.Instance.AddPiece(customPiece);
    }


    public static void AddPieceFortifiedEntranceRoofed()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_entrance_roofed");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 40,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 10,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedEntranceDrawbridgeRoofed()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_drawbridge_roofed");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[4]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 40,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 12,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Chain",
                    Amount = 8,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Iron",
                    Amount = 2,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        SetDoorEffects(gameObject, fortifiedOpen, fortifiedClose);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedWallRoofed()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_wall_roofed");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 40,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 8,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedWallStairsRoofed()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_wall_stairs_roofed");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 45,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 12,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedWallStairsAltRoofed()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_wall_stairs_alt_roofed");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 45,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 12,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedTjunctionRoofed()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_tjunction_roofed");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 50,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 16,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedCornerRoofed()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_corner_roofed");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 40,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 10,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedCornerAltRoofed()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_corner_roofed_alt");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 30,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 8,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedCornerCurvedRoofed()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_corner_curved_roofed");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 40,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 12,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }


    public static void AddPieceFortifiedGates()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_gates");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "forge",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[3]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 10,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 6,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Bronze",
                    Amount = 1,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        SetDoorEffects(gameObject, doorOpen, doorClose);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedDrawbridge(){
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_drawbridge");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "forge",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[4]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 8,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 20,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Chain",
                    Amount = 6,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Bronze",
                    Amount = 4,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        SetDoorEffects(gameObject, drawBridgeOC, drawBridgeOC);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedLiftGate()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_liftgate");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "forge",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[3]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 10,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 20,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Bronze",
                    Amount = 8,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        SetDoorEffects(gameObject, fortifiedOpen, fortifiedClose);
        PieceManager.Instance.AddPiece(customPiece);
    }


    public static void AddPieceInternalWall()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_internal_wall");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 20,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 8,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceInternalDoorway()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_internal_doorway");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 15,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 8,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceInternalCorner()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_internal_corner");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 20,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 8,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceInternalCurvedCorner()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_internal_corner_curved");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 20,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 8,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceInternalTjunction()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_internal_tjunction");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 30,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 12,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }


    public static void AddPieceFortifiedFence()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_fence");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[1]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 4,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedFenceLarge()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_fence_large");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[1]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 8,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedFenceWindowed()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_fence_windowed");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[1]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 6,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedFenceGate()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_fence_gate");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 4,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Bronze",
                    Amount = 1,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        SetDoorEffects(gameObject, doorOpen, doorClose);
        PieceManager.Instance.AddPiece(customPiece);
    }


    public static void AddPieceFortifiedSpikes()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_spikes");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_workbench",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 2,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedSpikesBronze()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_spikes_bronze");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "forge",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[3]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Bronze",
                    Amount = 2,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedSpikesIron()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_spikes_iron");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "forge",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[3]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Iron",
                    Amount = 2,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedSpikesBlackMetal()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_spikes_blackmetal");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_artisanstation",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[3]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "BlackMetal",
                    Amount = 2,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedSpikesFlametal()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_spikes_flametal");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "blackforge",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[3]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Flametal",
                    Amount = 2,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedSpikesBloodgold()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_spikes_gold");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "piece_FrostFoundry",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[3]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Gold",
                    Amount = 2,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        PieceManager.Instance.AddPiece(customPiece);
    }


    public static void AddPieceFortifiedBrazierSmall()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_brazier_small");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "forge",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Lighting },
            Requirements = new RequirementConfig[3]
            {
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Iron",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Coal",
                    Amount = 5,
                    Recover = false
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        Fireplace component2 = gameObject.GetComponent<Fireplace>();
        if (component2)
        {
            component2.m_fuelItem = fuelCoal;
            component2.m_fuelAddedEffects = addFuel;
            //component2.m_firePrefab = firePrefab;
        }
        else
        {
            Jotunn.Logger.LogWarning("[CorewoodFortified] mm_corewood_fortified_brazier_small has no Fireplace component, skipping its fuel setup.");
        }
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFortifiedBrazierLarge()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_brazier_large");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            CraftingStation = "forge",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Lighting },
            Requirements = new RequirementConfig[3]
            {
                new RequirementConfig
                {
                    Item = "Wood",
                    Amount = 4,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Iron",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Coal",
                    Amount = 5,
                    Recover = false
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        Fireplace component2 = gameObject.GetComponent<Fireplace>();
        if (component2)
        {
            component2.m_fuelItem = fuelCoal;
            component2.m_fuelAddedEffects = addFuel;
        }
        else
        {
            Jotunn.Logger.LogWarning("[CorewoodFortified] mm_corewood_fortified_brazier_large has no Fireplace component, skipping its fuel setup.");
        }
        PieceManager.Instance.AddPiece(customPiece);
    }


    public static void AddPieceFortifiedBallista()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_turret");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            Name = "$piece_mm_corewood_fortified_turret",
            Description = "$piece_mm_corewood_fortified_turret_description",
            CraftingStation = "forge",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[3]
            {
                new RequirementConfig
                {
                    Item = "BlackMetal",
                    Amount = 10,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "YggdrasilWood",
                    Amount = 10,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "MechanicalSpring",
                    Amount = 3,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        SetTurretFromVanilla(gameObject);
        TurretSettings.Init(gameObject);
        TurretAutoReload.Init(gameObject);
        AddTurretScanSound(gameObject);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceFlameTrap()
    {
        GameObject gameObject = AddTrapPiece("mm_corewood_flame_trap", "BlackMetal", 4, "Wood", 6, MainConfig.flameTrapTriggerRadius);
        TrapLoopSound.AddTo(gameObject, "sfx_flames_cwf", MainConfig.flameTrapSoundVolume);
    }

    public static void AddPieceSawWall()
    {
        GameObject gameObject = AddTrapPiece("mm_corewood_saw_wall", "BlackMetal", 4, "Wood", 6, MainConfig.sawWallTriggerRadius);
        TrapLoopSound.AddTo(gameObject, "sfx_saw_cwf", MainConfig.sawWallSoundVolume);
    }

    public static void AddPieceSpikeFloor()
    {
        GameObject gameObject = AddTrapPiece("mm_corewood_spike_floor", "BlackMetal", 4, "Stone", 8, MainConfig.spikeFloorTriggerRadius);
        TrapStabSound.AddTo(gameObject, "sfx_spikes_cwf", MainConfig.spikeFloorSoundVolume);
    }

    // Not a trap: it is powered by a fence controller rather than switched by a trap controller
    public static void AddPieceElectricFence()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_electric_fence");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            Name = "$piece_mm_corewood_electric_fence",
            Description = "$piece_mm_corewood_electric_fence_description",
            CraftingStation = "piece_magetable",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Eitr",
                    Amount = 2,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildMetal;
        SetWearEffects(gameObject, hitMetal, breakMetal);
        ElectricFence.AddTo(gameObject, GetWorkbenchPlaceEffect());
        ElectricFenceArcs.AddTo(gameObject);
        TrapLoopSound.AddTo(gameObject, "sfx_electric_fence_cwf", MainConfig.electricFenceSoundVolume);
        PieceManager.Instance.AddPiece(customPiece);
    }

    public static void AddPieceElectricFence4m()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_electric_fence_4m");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            Name = "$piece_mm_corewood_electric_fence_4m",
            Description = "$piece_mm_corewood_electric_fence_description",
            CraftingStation = "piece_magetable",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Eitr",
                    Amount = 4,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildMetal;
        SetWearEffects(gameObject, hitMetal, breakMetal);
        ElectricFence.AddTo(gameObject, GetWorkbenchPlaceEffect());
        ElectricFenceArcs.AddTo(gameObject);
        TrapLoopSound.AddTo(gameObject, "sfx_electric_fence_cwf", MainConfig.electricFenceSoundVolume);
        PieceManager.Instance.AddPiece(customPiece);
    }

    // Switches with the vanilla ward's on and off effects
    public static void AddPieceFenceController()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fence_controller_new");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            Name = "$piece_mm_corewood_fence_controller",
            Description = "$piece_mm_corewood_fence_controller_description",
            CraftingStation = "piece_magetable",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[4]
            {
                new RequirementConfig
                {
                    Item = "RoundLog",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Crystal",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Eitr",
                    Amount = 2,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Iron",
                    Amount = 4,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildMetal;
        SetWearEffects(gameObject, hitMetal, breakMetal);
        GameObject wardPrefab = PrefabManager.Cache.GetPrefab<GameObject>("guard_stone");
        PrivateArea ward = wardPrefab ? wardPrefab.GetComponent<PrivateArea>() : null;
        FenceController.AddTo(gameObject, ward ? ward.m_activateEffect : new EffectList(), ward ? ward.m_deactivateEffect : new EffectList());
        gameObject.GetComponent<FenceController>().m_areaMarker = SetMarkerFrom(gameObject, ward ? ward.m_areaMarker : null);
        TrapLoopSound.AddTo(gameObject, "sfx_fence_controller_cwf", MainConfig.fenceControllerSoundVolume);
        PieceManager.Instance.AddPiece(customPiece);
    }

    // The clank of a workbench being placed, which traps and electric fences make as they switch on and off
    private static EffectList GetWorkbenchPlaceEffect()
    {
        GameObject workbench = PrefabManager.Cache.GetPrefab<GameObject>("piece_workbench");
        return workbench ? workbench.GetComponent<Piece>().m_placeEffect : new EffectList();
    }

    // The traps' damage and visuals are already on the prefab (Aoe, and an Animator or particles);
    // TrapEnabler only switches them on and off. They clank like a workbench being placed as they do
    private static GameObject AddTrapPiece(string prefabName, string item1, int amount1, string item2, int amount2, BepInEx.Configuration.ConfigEntry<float> triggerRadius)
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>(prefabName);
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            Name = "$piece_" + prefabName,
            Description = "$piece_" + prefabName + "_description",
            CraftingStation = "piece_artisanstation",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = item1,
                    Amount = amount1,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = item2,
                    Amount = amount2,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);
        EffectList switchEffect = GetWorkbenchPlaceEffect();
        TrapEnabler.AddTo(gameObject, switchEffect, switchEffect, triggerRadius);
        PieceManager.Instance.AddPiece(customPiece);
        return gameObject;
    }

    // Takes its on and off effects and its area marker's look from the vanilla ward
    public static void AddPieceTrapController()
    {
        GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_trap_controller");
        CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
        {
            Name = "$piece_mm_corewood_trap_controller",
            Description = "$piece_mm_corewood_trap_controller_description",
            CraftingStation = "piece_artisanstation",
            PieceTable = "_HammerPieceTable",
            Category = "CoreWood-Fortified",
            Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
            Requirements = new RequirementConfig[2]
            {
                new RequirementConfig
                {
                    Item = "Stone",
                    Amount = 20,
                    Recover = true
                },
                new RequirementConfig
                {
                    Item = "Coal",
                    Amount = 6,
                    Recover = true
                }
            }
        });
        gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
        SetWearEffects(gameObject, hitWood, breakWood);

        TrapController controller = gameObject.AddComponent<TrapController>();
        GameObject wardPrefab = PrefabManager.Cache.GetPrefab<GameObject>("guard_stone");
        PrivateArea ward = wardPrefab ? wardPrefab.GetComponent<PrivateArea>() : null;
        if (ward)
        {
            controller.m_activateEffect = ward.m_activateEffect;
            controller.m_deactivateEffect = ward.m_deactivateEffect;
        }
        else
        {
            Jotunn.Logger.LogWarning("[CorewoodFortified] Vanilla guard_stone not found, the trap controller will switch silently.");
        }
        controller.m_areaMarker = SetMarkerFrom(gameObject, ward ? ward.m_areaMarker : null);
        PieceManager.Instance.AddPiece(customPiece);
    }

    // Gives the piece's area markers the look of the source marker. A CircleProjector without a segment prefab
    // throws every frame it is active, so a marker that cannot get one is removed. Returns the first one kept
    private static CircleProjector SetMarkerFrom(GameObject piece, CircleProjector source)
    {
        CircleProjector kept = null;
        CircleProjector[] markers = piece.GetComponentsInChildren<CircleProjector>(includeInactive: true);
        if (markers.Length == 0)
        {
            Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no AreaMarker, its range will not be shown.");
            return null;
        }
        foreach (CircleProjector marker in markers)
        {
            if (source && source.m_prefab)
            {
                marker.m_prefab = source.m_prefab;
                marker.m_mask = source.m_mask;
                marker.m_nrOfSegments = source.m_nrOfSegments;
                marker.m_speed = source.m_speed;
                marker.m_sliceLines = source.m_sliceLines;
            }
            if (!marker.m_prefab)
            {
                Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " marker '" + marker.name + "' has no segment prefab, removing it so its range will not be shown.");
                UnityEngine.Object.DestroyImmediate(marker, allowDestroyingAssets: true);
                continue;
            }
            if (!kept)
            {
                kept = marker;
            }
        }
        return kept;
    }

    // Copies the ammo, targets, effects and marker look from the vanilla ballista. Only data is copied, so the
    // prefab's own model and positions (its taller ballista_base) are kept
    private static void SetTurretFromVanilla(GameObject piece)
    {
        Turret turret = piece.GetComponent<Turret>();
        if (!turret)
        {
            Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no Turret component, skipping its turret setup.");
            return;
        }
        GameObject vanillaPrefab = PrefabManager.Cache.GetPrefab<GameObject>("piece_turret");
        Turret vanilla = vanillaPrefab ? vanillaPrefab.GetComponent<Turret>() : null;
        if (!vanilla)
        {
            Jotunn.Logger.LogWarning("[CorewoodFortified] Vanilla piece_turret not found, " + piece.name + " will have no ammo or effects.");
            return;
        }

        turret.m_name = piece.GetComponent<Piece>().m_name;
        turret.m_defaultAmmo = vanilla.m_defaultAmmo;
        turret.m_ammoType = vanilla.m_ammoType;
        // Each ammo shows a bolt model on the turret, so the visual is this prefab's child of the same name,
        // not the vanilla turret's
        turret.m_allowedAmmo = new List<Turret.AmmoType>();
        foreach (Turret.AmmoType ammo in vanilla.m_allowedAmmo)
        {
            Transform visual = ammo.m_visual ? Utils.FindChild(piece.transform, ammo.m_visual.name) : null;
            if (ammo.m_visual && !visual)
            {
                Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no '" + ammo.m_visual.name + "' child, " + ammo.m_ammo.name + " will not show on the turret.");
            }
            turret.m_allowedAmmo.Add(new Turret.AmmoType
            {
                m_ammo = ammo.m_ammo,
                m_visual = visual ? visual.gameObject : null
            });
        }
        turret.m_configTargets = new List<Turret.TrophyTarget>(vanilla.m_configTargets);
        turret.m_maxConfigTargets = vanilla.m_maxConfigTargets;

        turret.m_shootEffect = vanilla.m_shootEffect;
        turret.m_addAmmoEffect = vanilla.m_addAmmoEffect;
        turret.m_reloadEffect = vanilla.m_reloadEffect;
        turret.m_warmUpStartEffect = vanilla.m_warmUpStartEffect;
        turret.m_newTargetEffect = vanilla.m_newTargetEffect;
        turret.m_lostTargetEffect = vanilla.m_lostTargetEffect;
        turret.m_setTargetEffect = vanilla.m_setTargetEffect;

        turret.m_markerHideTime = vanilla.m_markerHideTime;
        SetTurretMarker(piece, turret, vanilla);
    }

    // The turret sets the marker's radius to its view distance when it wakes, so only the look is copied.
    // A CircleProjector without a segment prefab throws every frame it is active (on the placement ghost too),
    // so a marker that cannot get one is removed, and the vanilla turret null-checks m_marker everywhere
    private static void SetTurretMarker(GameObject piece, Turret turret, Turret vanilla)
    {
        turret.m_marker = null;
        CircleProjector[] markers = piece.GetComponentsInChildren<CircleProjector>(includeInactive: true);
        if (markers.Length == 0)
        {
            Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no AreaMarker, its range will not be shown.");
            return;
        }

        CircleProjector source = vanilla.m_marker;
        if (!source || !source.m_prefab)
        {
            source = vanilla.GetComponentsInChildren<CircleProjector>(includeInactive: true).FirstOrDefault(projector => projector.m_prefab);
        }

        foreach (CircleProjector marker in markers)
        {
            if (source)
            {
                marker.m_prefab = source.m_prefab;
                marker.m_mask = source.m_mask;
                marker.m_nrOfSegments = source.m_nrOfSegments;
                marker.m_speed = source.m_speed;
                marker.m_sliceLines = source.m_sliceLines;
            }
            if (!marker.m_prefab)
            {
                Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " marker '" + marker.name + "' has no segment prefab, removing it so its range will not be shown.");
                UnityEngine.Object.DestroyImmediate(marker, allowDestroyingAssets: true);
                continue;
            }
            if (!turret.m_marker)
            {
                turret.m_marker = marker;
            }
        }
    }

    // The vanilla ballista is silent while it looks for a target, so it is given its own scanning loop
    private static void AddTurretScanSound(GameObject piece)
    {
        GameObject soundPrefab = PrefabManager.Instance.GetPrefab("sfx_turret_scan_cwf");
        if (!soundPrefab)
        {
            Jotunn.Logger.LogWarning("[CorewoodFortified] Sound prefab 'sfx_turret_scan_cwf' not found, " + piece.name + " will scan silently.");
            return;
        }
        piece.AddComponent<TurretScanSound>().m_soundPrefab = soundPrefab;
    }

    private static void SetWearEffects(GameObject piece, EffectList hit, EffectList destroyed)
    {
        WearNTear wearNTear = piece.GetComponent<WearNTear>();
        if (!wearNTear)
        {
            Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no WearNTear component, skipping its hit and break effects.");
            return;
        }
        wearNTear.m_destroyedEffect = destroyed;
        wearNTear.m_hitEffect = hit;
    }

    private static void SetDoorEffects(GameObject piece, EffectList open, EffectList close)
    {
        Door door = piece.GetComponent<Door>();
        if (!door)
        {
            Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no Door component, skipping its open and close effects.");
            return;
        }
        door.m_openEffects = open;
        door.m_closeEffects = close;
    }
}
