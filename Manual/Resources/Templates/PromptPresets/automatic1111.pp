{
  "LatentNodes": [
    {
      "$type": "Manual.Core.Nodes.PromptNode, Manual",
      "ParentWrap": null,
      "Position": "-152.9302215576172,124.09315490722656",
      "Fields": [
        {
          "Name": "Text",
          "Direction": 2,
          "Id": "ce6354a3-f676-4dde-aabb-7e6b18e50bdb",
          "IdSlot": null,
          "FieldValueId": null
        },
        {
          "Name": "Prompt",
          "Direction": 3,
          "Id": "e5fad46e-d50e-4b0b-96cf-079d8edc27e7",
          "IdSlot": null,
          "FieldValueId": "cute cat"
        }
      ],
      "NameType": "nodeType",
      "Id": "dcc613ce-38e8-44d1-802a-415e591b529f",
      "IdNode": 0,
      "IsVisible": true,
      "Name": "Prompt",
      "TitleColor": "#00000000",
      "PositionGlobalX": -152.93022,
      "PositionGlobalY": 124.093155,
      "SizeX": 576.846,
      "SizeY": 193.0
    },
    {
      "$type": "Manual.Core.Nodes.PromptNode, Manual",
      "ParentWrap": null,
      "Position": "-160.3931427001953,324.8676452636719",
      "Fields": [
        {
          "Name": "Text",
          "Direction": 2,
          "Id": "8004a1fc-678e-4c21-aa55-390b12cc5fed",
          "IdSlot": null,
          "FieldValueId": null
        },
        {
          "Name": "Prompt",
          "Direction": 3,
          "Id": "e36c428b-3b68-413a-8d83-6c4c3a640268",
          "IdSlot": null,
          "FieldValueId": "realistic, text, bad quality"
        }
      ],
      "NameType": "nodeType",
      "Id": "6b9c549b-a2a5-496a-8ee7-36034e4a7081",
      "IdNode": 0,
      "IsVisible": true,
      "Name": "Negative Prompt",
      "TitleColor": "#00000000",
      "PositionGlobalX": -160.39314,
      "PositionGlobalY": 324.86765,
      "SizeX": 585.38184,
      "SizeY": 193.0
    },
    {
      "$type": "Manual.Core.Nodes.PrincipledLatentNode, Manual",
      "Scripts": {},
      "ParentWrap": null,
      "Position": "498.83453369140625,44.66764831542969",
      "Fields": [
        {
          "Name": "Generation",
          "Direction": 2,
          "Id": "d6580bf9-0131-4028-b564-854455c12ce8",
          "IdSlot": null,
          "FieldValueId": null
        },
        {
          "Name": "Model",
          "Direction": 3,
          "Id": "ebb9548a-7b8d-4ce4-bee2-21c3f8f55832",
          "IdSlot": null,
          "FieldValueId": "Model"
        },
        {
          "Name": "Reference",
          "Direction": 0,
          "Id": "a5dfb68a-1222-4106-9a42-a2e412477c76",
          "IdSlot": null,
          "FieldValueId": null
        },
        {
          "Name": "Denoising Strength",
          "Direction": 3,
          "Id": "17c04973-e091-4b42-b938-fd3cd0218a7d",
          "IdSlot": null,
          "FieldValueId": 0.75
        },
        {
          "Name": "Prompt Positive",
          "Direction": 0,
          "Id": "e6565451-76e8-4252-bba3-ec2f07aab8ae",
          "IdSlot": null,
          "FieldValueId": null
        },
        {
          "Name": "Prompt Negative",
          "Direction": 0,
          "Id": "2628bc26-4be9-48ad-a93d-2b5dadd1cf72",
          "IdSlot": null,
          "FieldValueId": null
        },
        {
          "Name": "Resolution X",
          "Direction": 3,
          "Id": "5ce71a6f-5e56-41f7-b403-a022444ae484",
          "IdSlot": null,
          "FieldValueId": 512
        },
        {
          "Name": "Resolution Y",
          "Direction": 3,
          "Id": "161aea23-b44a-4d0c-afee-d2ef876a626f",
          "IdSlot": null,
          "FieldValueId": 512
        },
        {
          "Name": "Steps",
          "Direction": 3,
          "Id": "819df539-d82c-42bd-91b4-ab9e543e1933",
          "IdSlot": null,
          "FieldValueId": 20
        },
        {
          "Name": "CFG Scale",
          "Direction": 3,
          "Id": "a8dbd686-0873-4ff2-a622-d216d35c2593",
          "IdSlot": null,
          "FieldValueId": 5
        },
        {
          "Name": "Seed",
          "Direction": 3,
          "Id": "f7e52869-6481-4b88-82fa-2cf441f72f1e",
          "IdSlot": null,
          "FieldValueId": -1
        },
        {
          "Name": "Mask",
          "Direction": 0,
          "Id": "efd59125-18f5-4307-8eb0-b4004ccabcfe",
          "IdSlot": null,
          "FieldValueId": null
        },
        {
          "Name": "Blur",
          "Direction": 3,
          "Id": "1c7444c4-15b2-463c-a43c-d9ba2aea798f",
          "IdSlot": null,
          "FieldValueId": 4
        },
        {
          "Name": "Masked Content",
          "Direction": 3,
          "Id": "6dc34ae5-5c86-4c88-8acc-9d0b7fa094c9",
          "IdSlot": null,
          "FieldValueId": 1
        },
        {
          "Name": "Control",
          "Direction": 0,
          "Id": "3aa4a5f2-5f66-44e3-bde6-417eb7e3c903",
          "IdSlot": null,
          "FieldValueId": null
        },
        {
          "Name": "Use Runpod",
          "Direction": 3,
          "Id": "87ad7121-c9d2-4885-8a64-aad5bc02c754",
          "IdSlot": null,
          "FieldValueId": false
        }
      ],
      "NameType": "nodeType",
      "Id": "1da2e29c-8bc6-4cf5-a8c2-a76698a5f81e",
      "IdNode": 0,
      "IsVisible": true,
      "Name": "Principled Latent",
      "TitleColor": "#00000000",
      "PositionGlobalX": 498.83453,
      "PositionGlobalY": 44.66765,
      "SizeX": 128.0,
      "SizeY": 443.09833
    },
    {
      "$type": "Manual.Core.Nodes.OutputNode, Manual",
      "ParentWrap": null,
      "Position": "752.25,45.33529281616211",
      "Fields": [
        {
          "Name": "Result",
          "Direction": 0,
          "Id": "ced19a9c-0eb7-4f45-9b99-f5399b8268e9",
          "IdSlot": null,
          "FieldValueId": null
        },
        {
          "Name": "Target",
          "Direction": 1,
          "Id": "a92e3179-e415-46b3-b965-a9666b24de08",
          "IdSlot": null,
          "FieldValueId": {
            "$type": "Manual.Core.ManualSerialization, Manual",
            "TypeId": "Manual.Objects.Layer, Manual, Version=0.9.2.0, Culture=neutral, PublicKeyToken=null",
            "Name": "Square"
          }
        }
      ],
      "NameType": "nodeType",
      "Id": "305c76f2-d7c4-428f-9229-73a555723993",
      "IdNode": 0,
      "IsVisible": true,
      "Name": "Output",
      "TitleColor": "#00000000",
      "PositionGlobalX": 752.25,
      "PositionGlobalY": 45.335293,
      "SizeX": 128.0,
      "SizeY": 130.15367
    },
    {
      "$type": "Manual.Core.Nodes.LayerNode, Manual",
      "ParentWrap": null,
      "Position": "228.0701904296875,-18.010347366333008",
      "Fields": [
        {
          "Name": "Layer",
          "Direction": 2,
          "Id": "620a7296-823c-47d5-8c23-aca3733d41a4",
          "IdSlot": null,
          "FieldValueId": null
        },
        {
          "Name": "LayerRef",
          "Direction": 3,
          "Id": "48ab5b1f-5da8-4fe9-bc5e-ca389d6903ae",
          "IdSlot": null,
          "FieldValueId": {
            "$type": "Manual.Core.ManualSerialization, Manual",
            "TypeId": "Manual.Objects.Layer, Manual, Version=0.9.2.0, Culture=neutral, PublicKeyToken=null",
            "Name": "Square"
          }
        },
        {
          "Name": "Normalize",
          "Direction": 3,
          "Id": "5009026b-1052-4851-a292-b70addf81551",
          "IdSlot": null,
          "FieldValueId": false
        }
      ],
      "NameType": "nodeType",
      "Id": "f7e50d65-2705-445e-912e-7072f9457243",
      "IdNode": 0,
      "IsVisible": true,
      "Name": "Layer",
      "TitleColor": "#00000000",
      "PositionGlobalX": 228.07019,
      "PositionGlobalY": -18.010347,
      "SizeX": 128.0,
      "SizeY": 133.6544
    }
  ],
  "LineConnections": [
    {
      "OutputId": "d6580bf9-0131-4028-b564-854455c12ce8",
      "InputId": "ced19a9c-0eb7-4f45-9b99-f5399b8268e9",
      "OutputNodeId": "1da2e29c-8bc6-4cf5-a8c2-a76698a5f81e",
      "InputNodeId": "305c76f2-d7c4-428f-9229-73a555723993",
      "Id": "5e29368e-bad1-4ab4-b41f-d34bd23c8a61",
      "PositionGlobalX": 690.83453,
      "PositionGlobalY": 86.66765,
      "PositionGlobalEndX": 61.415466,
      "PositionGlobalEndY": 0.6676445
    },
    {
      "OutputId": "ce6354a3-f676-4dde-aabb-7e6b18e50bdb",
      "InputId": "e6565451-76e8-4252-bba3-ec2f07aab8ae",
      "OutputNodeId": "dcc613ce-38e8-44d1-802a-415e591b529f",
      "InputNodeId": "1da2e29c-8bc6-4cf5-a8c2-a76698a5f81e",
      "Id": "8f669846-941c-436e-bdd1-80c64083eb94",
      "PositionGlobalX": 423.91577,
      "PositionGlobalY": 166.09315,
      "PositionGlobalEndX": 74.91876,
      "PositionGlobalEndY": 36.574493
    },
    {
      "OutputId": "8004a1fc-678e-4c21-aa55-390b12cc5fed",
      "InputId": "2628bc26-4be9-48ad-a93d-2b5dadd1cf72",
      "OutputNodeId": "6b9c549b-a2a5-496a-8ee7-36034e4a7081",
      "InputNodeId": "1da2e29c-8bc6-4cf5-a8c2-a76698a5f81e",
      "Id": "deb716ac-f486-4bae-8302-0cc106c7d775",
      "PositionGlobalX": 424.9887,
      "PositionGlobalY": 366.86765,
      "PositionGlobalEndX": 73.845825,
      "PositionGlobalEndY": -134.2
    }
  ],
  "CurrentExecutingNode": null,
  "Id": "cf940752-8369-4c25-848f-a722882ee14b",
  "Progress": 0.0,
  "Name": "PromptPreset",
  "CanvasMatrix": "0.721632996678387,0,0,0.721632996678387,218.54923252112698,164.08065545465863",
  "GenerateCommand": {
    "$type": "CommunityToolkit.Mvvm.Input.AsyncRelayCommand, CommunityToolkit.Mvvm",
    "ExecutionTask": null,
    "CanBeCanceled": false,
    "IsCancellationRequested": false,
    "IsRunning": false
  }
}