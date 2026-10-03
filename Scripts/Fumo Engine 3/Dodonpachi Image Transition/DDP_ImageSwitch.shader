Shader "UI/Custom/GridDisplaceFallingLoopless"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FlashColor ("Piece Flash/Fade Color", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        _GridSegments ("Grid Segments (X, Y)", Vector) = (9, 12, 0, 0)
        _State ("State (-1 to 1)", Range(-1, 1)) = 0.0
        _GravityScale ("Gravity Scale", Float) = 2.5
        _SpreadScale ("Horizontal Spread Scale", Float) = 0.4
        _RotationScale ("Max Tile Rotation (Rad)", Float) = 3.14
        _FlashWindow ("Tile Fall/Fade Window", Range(0.05, 0.5)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;

            fixed4 _Color;
            fixed4 _FlashColor;
            fixed4 _TextureSampleAdd;

            float4 _ClipRect;
            float4 _MainTex_ST;

            float2 _GridSegments;
            float _State;
            float _GravityScale;
            float _SpreadScale;
            float _RotationScale;
            float _FlashWindow;

            float2 hash22(float2 p)
            {
                float3 p3 =
                    frac(
                        p.xyx *
                        float3(0.1031, 0.1030, 0.0973)
                    );

                p3 +=
                    dot(
                        p3,
                        p3.yzx + 33.33
                    );

                return
                    frac(
                        (p3.xx + p3.yz) * p3.zy
                    ) * 2.0 - 1.0;
            }

            float2 rotate2D(float2 p, float angle)
            {
                float s;
                float c;

                sincos(angle, s, c);

                return float2(
                    p.x * c - p.y * s,
                    p.x * s + p.y * c
                );
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 pixelUV = IN.texcoord;

                float2 numCells =
                    max(
                        _GridSegments,
                        float2(1.0, 1.0)
                    );

                float2 cellSize =
                    1.0 / numCells;

                float state =
                    clamp(
                        _State,
                        -1.0,
                        1.0
                    );

                bool entering =
                    state < 0.0;

                float animationTime =
                    entering
                        ? state + 1.0
                        : state;

                float flashWindow =
                    max(
                        0.001,
                        _FlashWindow
                    );

                float displacementProgress =
                    entering
                        ? 1.0 - animationTime
                        : animationTime;

                float baseFall =
                    displacementProgress *
                    displacementProgress *
                    _GravityScale;

                float2 unfallenUV;

                if (entering)
                {
                    unfallenUV =
                        float2(
                            pixelUV.x,
                            pixelUV.y - baseFall
                        );
                }
                else
                {
                    unfallenUV =
                        float2(
                            pixelUV.x,
                            pixelUV.y + baseFall
                        );
                }

                float2 approxCellIdx =
                    floor(
                        unfallenUV * numCells
                    );

                float2 halfCell =
                    cellSize * 0.5;

                float tileRadius =
                    length(halfCell);

                fixed4 finalColor =
                    fixed4(0, 0, 0, 0);

                float bestProgress = 2.0;
                float bestPriority = -1.0;
                bool foundTile = false;

                [unroll]
                for (int x = -2; x <= 2; x++)
                {
                    [unroll]
                    for (int y = -2; y <= 2; y++)
                    {
                        float2 cellIdx =
                            approxCellIdx +
                            float2(x, y);

                        if (
                            cellIdx.x < 0.0 ||
                            cellIdx.x >= numCells.x ||
                            cellIdx.y < 0.0 ||
                            cellIdx.y >= numCells.y
                        )
                        {
                            continue;
                        }

                        float yNormalized =
                            cellIdx.y /
                            max(
                                1.0,
                                numCells.y - 1.0
                            );

                        float tileStart;

                        if (entering)
                        {
                            tileStart =
                                yNormalized *
                                (1.0 - flashWindow);
                        }
                        else
                        {
                            tileStart =
                                (1.0 - yNormalized) *
                                (1.0 - flashWindow);
                        }

                        if (!entering)
                        {
                            float tileEnd =
                                tileStart +
                                flashWindow;

                            if (animationTime >= tileEnd)
                            {
                                continue;
                            }
                        }

                        float tileProgress =
                            saturate(
                                (animationTime - tileStart) /
                                flashWindow
                            );

                        if (
                            entering &&
                            animationTime < tileStart
                        )
                        {
                            continue;
                        }

                        float priority =
                            entering
                                ? (
                                    (numCells.y - 1.0 - cellIdx.y) *
                                    numCells.x +
                                    cellIdx.x
                                )
                                : (
                                    cellIdx.y *
                                    numCells.x +
                                    cellIdx.x
                                );

                        bool canCompete =
                            !foundTile ||
                            tileProgress < bestProgress ||
                            (
                                abs(tileProgress - bestProgress) < 0.001 &&
                                priority > bestPriority
                            );

                        if (!canCompete)
                        {
                            continue;
                        }

                        float2 originalCenter =
                            (cellIdx + 0.5) *
                            cellSize;

                        float2 randVal =
                            hash22(cellIdx);

                        float effectProgress =
                            entering
                                ? 1.0 - tileProgress
                                : tileProgress;

                        float fallProgress =
                            effectProgress *
                            effectProgress;

                        float xDrift =
                            randVal.x *
                            _SpreadScale;

                        float rotDir =
                            randVal.y;

                        float normX =
                            (cellIdx.x + 0.5) /
                            numCells.x;

                        float centerOffset =
                            normX - 0.5;

                        float outwardPush =
                            sign(centerOffset) *
                            (0.5 - abs(centerOffset)) *
                            0.3;

                        float tileFallDistance =
                            fallProgress *
                            _GravityScale;

                        float totalXOffset =
                            (xDrift + outwardPush) *
                            fallProgress;

                        float verticalOffset;

                        if (entering)
                        {
                            verticalOffset =
                                _GravityScale *
                                fallProgress;
                        }
                        else
                        {
                            verticalOffset =
                                -tileFallDistance;
                        }

                        float2 displacedCenter =
                            originalCenter +
                            float2(
                                totalXOffset,
                                verticalOffset
                            );

                        float2 delta =
                            pixelUV -
                            displacedCenter;

                        if (
                            abs(delta.x) > tileRadius ||
                            abs(delta.y) > tileRadius
                        )
                        {
                            continue;
                        }

                        float angle =
                            rotDir *
                            _RotationScale *
                            fallProgress;

                        if (entering)
                        {
                            angle = -angle;
                        }

                        float2 unrotatedDelta =
                            rotate2D(
                                delta,
                                -angle
                            );

                        if (
                            abs(unrotatedDelta.x) > halfCell.x ||
                            abs(unrotatedDelta.y) > halfCell.y
                        )
                        {
                            continue;
                        }

                        float2 localTileUV =
                            unrotatedDelta +
                            halfCell;

                        float2 textureSampleUV =
                            (cellIdx * cellSize) +
                            localTileUV;

                        fixed4 texCol =
                            (
                                tex2D(
                                    _MainTex,
                                    textureSampleUV
                                ) +
                                _TextureSampleAdd
                            ) *
                            IN.color;

                        float visibility =
                            entering
                                ? tileProgress
                                : 1.0 - tileProgress;

                        texCol.rgb =
                            lerp(
                                _FlashColor.rgb,
                                texCol.rgb,
                                visibility
                            );

                        texCol.a *=
                            visibility;

                        finalColor =
                            texCol;

                        bestProgress =
                            tileProgress;

                        bestPriority =
                            priority;

                        foundTile =
                            true;
                    }
                }

                if (!foundTile)
                {
                    discard;
                }

                #ifdef UNITY_UI_CLIP_RECT

                finalColor.a *=
                    UnityGet2DClipping(
                        IN.worldPosition.xy,
                        _ClipRect
                    );

                #endif

                #ifdef UNITY_UI_ALPHACLIP

                clip(
                    finalColor.a - 0.001
                );

                #endif

                return finalColor;
            }

            ENDCG
        }
    }
}