using UnityEngine;
using UnityEngine.UIElements;

namespace MouseKeeper
{
    public sealed partial class PlanningController
    {
        readonly Texture2D[] specialtyIcons = new Texture2D[6];
        readonly Label[] specialtySlots = new Label[ExpeditionSession.SquadSize];
        readonly Image[] specialtySlotIcons = new Image[ExpeditionSession.SquadSize];
        readonly VisualElement[] specialtyBadges = new VisualElement[6];
        Label specialtyCost;
        static readonly string[] SpecialtyNames = { "Sin especialidad", "DPS", "Médico", "Tanque", "Cerrajero", "Bardo" };
        static readonly string[] SpecialtyDescriptions = {
            "", "+20% daño y +15% velocidad de recarga.",
            "Regenera 1% de vida por segundo hasta 50%; cura 5% de vida máxima por $1 al compañero con menor porcentaje de vida bajo 70% en exploración.",
            "+250% vida; atrae inicialmente a las ratas y protege al equipo.",
            "Una vez por piso, evacúa al equipo si alguien está en estado crítico.",
            "Una vez por piso, cura 30% y aumenta movimiento y ataque 25% hasta despejar."
        };

        void BuildSpecialtyControls()
        {
            VisualElement column = roster.parent;
            var header = new Label("ESPECIALIDADES · 10% DEL CONTRATO CADA UNA");
            header.AddToClassList("section-title");
            var hint = new Label("Arrastra una marca al agente o selecciónalo y pulsa la marca.");
            hint.AddToClassList("hint");
            var bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.flexWrap = Wrap.Wrap;
            for (int i = 1; i <= 5; i++)
            {
                int index = i;
                Vector2 pointerStart = Vector2.zero;
                specialtyIcons[i] = CreateSpecialtyIcon(i);
                var badge = new VisualElement();
                badge.style.width = 42; badge.style.height = 42;
                badge.style.marginRight = 4; badge.style.marginBottom = 4;
                badge.style.backgroundImage = new StyleBackground(specialtyIcons[i]);
                badge.tooltip = SpecialtyNames[i] + " — " + SpecialtyDescriptions[i];
                badge.RegisterCallback<PointerDownEvent>(evt => {
                    if (evt.button != 0) return;
                    pointerStart = evt.position;
                    badge.CapturePointer(evt.pointerId);
                    evt.StopPropagation();
                });
                badge.RegisterCallback<PointerUpEvent>(evt => {
                    if (!badge.HasPointerCapture(evt.pointerId)) return;
                    badge.ReleasePointer(evt.pointerId);
                    int destination = selectedSlot;
                    bool overSlot = false;
                    for (int slot = 0; slot < specialtySlots.Length; slot++)
                        if (root.Q<VisualElement>("slot-" + slot).worldBound.Contains(evt.position)) { destination = slot; overSlot = true; break; }
                    if (!overSlot && ((Vector2)evt.position - pointerStart).sqrMagnitude > 36f) return;
                    AssignSpecialty(destination, (Specialty)index);
                    evt.StopPropagation();
                });
                specialtyBadges[i] = badge;
                bar.Add(badge);
            }
            specialtyCost = new Label();
            specialtyCost.AddToClassList("hint");
            int insertion = column.IndexOf(roster);
            column.Insert(insertion++, header);
            column.Insert(insertion++, hint);
            column.Insert(insertion++, bar);
            column.Insert(insertion, specialtyCost);
            for (int i = 0; i < specialtySlots.Length; i++)
            {
                int slot = i;
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                specialtySlotIcons[i] = new Image();
                specialtySlotIcons[i].style.width = 24;
                specialtySlotIcons[i].style.height = 24;
                specialtySlotIcons[i].style.marginRight = 5;
                specialtySlots[i] = new Label();
                specialtySlots[i].style.flexGrow = 1;
                specialtySlots[i].AddToClassList("hint");
                var clear = new Button(() => AssignSpecialty(slot, Specialty.None)) { text = "×", tooltip = "Quitar especialidad" };
                clear.AddToClassList("money-button");
                row.Add(specialtySlotIcons[i]);
                row.Add(specialtySlots[i]);
                row.Add(clear);
                root.Q<VisualElement>("slot-" + i).Add(row);
            }
        }

        void AssignSpecialty(int slot, Specialty specialty)
        {
            CampaignData data = ExpeditionSession.Data;
            if (data == null || data.active) return;
            if (specialty != Specialty.None)
                for (int i = 0; i < data.specialties.Length; i++)
                    if (i != slot && data.specialties[i] == specialty) data.specialties[i] = Specialty.None;
            data.specialties[slot] = specialty;
            ExpeditionSession.Save();
            Refresh();
        }

        void RefreshSpecialtyControls()
        {
            if (specialtyCost == null) return;
            int count = 0;
            for (int i = 0; i < specialtySlots.Length; i++)
            {
                Specialty specialty = ExpeditionSession.Data.specialties[i];
                if (specialty != Specialty.None) count++;
                specialtySlots[i].text = specialty == Specialty.None ? "Sin especialidad" : "";
                specialtySlotIcons[i].image = specialtyIcons[(int)specialty];
                specialtySlotIcons[i].style.display = specialty == Specialty.None ? DisplayStyle.None : DisplayStyle.Flex;
                specialtySlotIcons[i].tooltip = SpecialtyNames[(int)specialty] + " — " + SpecialtyDescriptions[(int)specialty];
            }
            specialtyCost.text = "Recompensa del contrato para ti: " + (100 - count * 10) + "%";
            for (int i = 1; i <= 5; i++)
                specialtyBadges[i].style.opacity = System.Array.IndexOf(ExpeditionSession.Data.specialties, (Specialty)i) >= 0 ? 0.45f : 1f;
        }

        internal static Texture2D CreateSpecialtyIcon(int kind)
        {
            var texture = new Texture2D(48, 48, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            Color background = new Color(0.18f, 0.15f, 0.12f);
            Color[] colors = { Color.white, new Color(1f, .42f, .22f), new Color(.4f, .9f, .62f),
                new Color(.45f, .72f, 1f), new Color(1f, .76f, .3f), new Color(.8f, .55f, 1f) };
            Color ink = colors[kind];
            for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++)
            {
                float dx = x - 23.5f, dy = y - 23.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                bool mark = kind == 1 ? (Mathf.Abs(r - 11f) < 2f || (Mathf.Abs(dx) < 2f && Mathf.Abs(dy) < 17f) || (Mathf.Abs(dy) < 2f && Mathf.Abs(dx) < 17f)) :
                    kind == 2 ? (Mathf.Abs(dx) < 4f && Mathf.Abs(dy) < 14f || Mathf.Abs(dy) < 4f && Mathf.Abs(dx) < 14f) :
                    kind == 3 ? (dy < 14f && dy > -15f && Mathf.Abs(dx) < (dy < -5f ? (dy + 15f) : 12f) && (Mathf.Abs(dx) > 8f || dy > 10f || dy < -8f)) :
                    kind == 4 ? (Mathf.Abs(Mathf.Sqrt((dx + 6f) * (dx + 6f) + (dy - 5f) * (dy - 5f)) - 6f) < 2.5f || (dx > -1f && dx < 14f && Mathf.Abs(dy + dx - 1f) < 2f) || (dx > 8f && dx < 13f && dy < -7f && dy > -13f)) :
                    (dx > 0f && dx < 5f && dy > -5f && dy < 14f || dy > 9f && dy < 14f && dx > 0f && dx < 13f || (dx + 4f) * (dx + 4f) + (dy + 8f) * (dy + 8f) < 25f);
                texture.SetPixel(x, y, r > 23f ? Color.clear : mark || r > 20f ? ink : background);
            }
            texture.Apply();
            return texture;
        }

        void OnDestroy()
        {
            foreach (Texture2D icon in specialtyIcons) if (icon != null) Destroy(icon);
        }
    }
}
