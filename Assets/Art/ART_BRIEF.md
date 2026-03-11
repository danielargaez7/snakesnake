# Belly Full - Art Brief

## General Style
- **Perspective**: Top-down / bird's eye view (looking straight down at ground)
- **Audience**: Ages 3-5, Pre-K to Kindergarten
- **Feel**: Bright, warm, friendly, painterly/watercolor, NOT flat/minimal
- **Palette**: Saturated but soft — greens, yellows, warm oranges, sky blues
- **Format**: PNG with transparency (except background)
- **Resolution**: Target 1920x1080 display, sprites at 100 pixels-per-unit

---

## BACKGROUND

### `Backgrounds/bg_meadow.png` (1920 x 1080)
- Top-down grassy meadow, fills entire screen
- Bright green grass with subtle texture variation
- A few tiny baked-in daisies, clover patches, soft dirt spots for visual interest
- Edges slightly darker/bushier (hedgehogs "escape into bushes" during Ball Blast)
- Center area relatively clean — that's where gameplay happens
- No UI elements baked in
- Warm, inviting, like a picnic blanket or garden floor
- NO horizon, NO sky, NO perspective depth

---

## SNAKE SPRITES

Both snakes seen from directly above — round head, body trailing behind.

### `Sprites/Snake/snake_p1_head.png` (128 x 128)
- **Player 1 snake head** — top-down view
- Green color (#4DCC4D range)
- Big round head with two large expressive eyes looking upward
- Friendly smile/mouth
- Slightly cartoonish, chunky proportions

### `Sprites/Snake/snake_p2_head.png` (128 x 128)
- **Player 2 snake head** — same style as P1
- Blue color (#4D80E6 range)
- Same proportions, different color and maybe slightly different eye expression

### `Sprites/Snake/snake_body_segment.png` (64 x 64)
- **Body segment** — round/oval, semi-transparent center
- Neutral color (will be tinted green/blue per player via code)
- The transparent belly area is where ball count shows through
- Think of it like a bubble or pouch shape

### `Sprites/Snake/snake_belly_ball.png` (32 x 32)
- **Ball visible inside belly** — small, seen through transparent body
- Same as the regular ball but smaller and slightly muted/behind glass look
- Shown inside the snake body to represent belly count

---

## FIELD OBJECT SPRITES

All seen from top-down. These roam the shared field.

### `Sprites/Objects/ball.png` (64 x 64)
- **Edible ball** (addition object)
- Round, warm yellow/orange (#FFD633 range)
- Friendly, slightly shiny, maybe a subtle star highlight
- Simple and immediately readable as "food" for the snake

### `Sprites/Objects/hedgehog.png` (80 x 80)
- **Hedgehog** (subtraction object) — top-down view
- Round spiky back seen from above, brown/tan body (#996633 range)
- Cute small face peeking out from center (tiny eyes, tiny nose)
- Spikes radiating outward but soft/rounded — NOT scary
- Should read as "prickly but friendly"

### `Sprites/Objects/flower.png` (48 x 48)
- **Decorative flower** (no gameplay function, just ambiance)
- Soft pink/purple petals seen from above (#E666B2 range)
- Simple 5-petal daisy shape
- Gently sways in code, so centered pivot point

---

## UI SPRITES

### `Sprites/UI/energy_bar_frame.png` (300 x 40)
- **Energy bar border/frame**
- Rounded rectangle outline
- Warm gold or wood-grain border
- Transparent interior (fill is done in code with colored Image)

### `Sprites/UI/energy_bar_fill.png` (8 x 32)
- **Energy bar fill texture** — tileable horizontally
- Gradient from green (low) to golden yellow (full)
- Or just a solid bright color (code handles fill amount)

### `Sprites/UI/energy_bar_glow.png` (300 x 40)
- **Energy bar glow overlay** — same size as frame
- Soft golden glow/bloom effect
- Used at low alpha, pulses when bar is nearly full

### `Sprites/UI/crown.png` (64 x 64)
- **Crown icon** for win tracking
- Classic golden crown, simple and bold
- Sparkly/shiny, celebratory feel
- 3 shown per player (lit up when earned, dim when not)

### `Sprites/UI/crown_dim.png` (64 x 64)
- **Unearned crown** — same shape as above
- Greyed out or silhouette version

### `Sprites/UI/belly_panel_p1.png` (200 x 120)
- **P1 HUD background panel** — top-left corner
- Semi-transparent green-tinted rounded rectangle
- Holds belly count number and equation text

### `Sprites/UI/belly_panel_p2.png` (200 x 120)
- **P2 HUD background panel** — top-right corner
- Semi-transparent blue-tinted rounded rectangle
- Same layout, different color

### `Sprites/UI/equation_bubble.png` (160 x 60)
- **Equation display bubble** (shows "+2" or "-1")
- Speech-bubble or thought-bubble shape
- White/cream fill with soft border
- Used for both players (tinted per player in code)

---

## EFFECT SPRITES

### `Sprites/Effects/belly_ache_stars.png` (48 x 48)
- **Stars/swirls for belly ache state**
- Cartoon dizzy stars circling
- Yellow stars on transparent background
- Will be rotated/animated in code

### `Sprites/Effects/blast_glow.png` (256 x 256)
- **Snake glow during Ball Blast**
- Soft radial glow, warm golden/white
- Applied as additive overlay around snake during blast mode

### `Sprites/Effects/eat_particle.png` (32 x 32)
- **Particle for eating feedback**
- Small sparkle/pop shape
- White/yellow, will be used in particle system

### `Sprites/Effects/crown_burst.png` (256 x 256)
- **Crown award celebration burst**
- Radial confetti/sparkle burst
- Gold and colorful, celebratory

---

## BALL BLAST SPECIFIC

### `Sprites/Effects/blast_countdown_bg.png` (400 x 400)
- **Background circle for 3-2-1 countdown**
- Bold circular shape, maybe with radiating lines
- Semi-transparent dark overlay to focus attention
- Numbers rendered in code via TMPro on top

---

## SUMMARY TABLE

| File | Size | Description |
|------|------|-------------|
| `Backgrounds/bg_meadow.png` | 1920x1080 | Top-down meadow playfield |
| `Sprites/Snake/snake_p1_head.png` | 128x128 | Green snake head (P1) |
| `Sprites/Snake/snake_p2_head.png` | 128x128 | Blue snake head (P2) |
| `Sprites/Snake/snake_body_segment.png` | 64x64 | Tintable body segment |
| `Sprites/Snake/snake_belly_ball.png` | 32x32 | Ball visible in belly |
| `Sprites/Objects/ball.png` | 64x64 | Yellow edible ball |
| `Sprites/Objects/hedgehog.png` | 80x80 | Top-down cute hedgehog |
| `Sprites/Objects/flower.png` | 48x48 | Decorative flower |
| `Sprites/UI/energy_bar_frame.png` | 300x40 | Energy bar border |
| `Sprites/UI/energy_bar_fill.png` | 8x32 | Energy bar fill (tileable) |
| `Sprites/UI/energy_bar_glow.png` | 300x40 | Energy bar glow overlay |
| `Sprites/UI/crown.png` | 64x64 | Golden crown (earned) |
| `Sprites/UI/crown_dim.png` | 64x64 | Grey crown (unearned) |
| `Sprites/UI/belly_panel_p1.png` | 200x120 | P1 HUD panel (green) |
| `Sprites/UI/belly_panel_p2.png` | 200x120 | P2 HUD panel (blue) |
| `Sprites/UI/equation_bubble.png` | 160x60 | Equation display bubble |
| `Sprites/Effects/belly_ache_stars.png` | 48x48 | Dizzy stars effect |
| `Sprites/Effects/blast_glow.png` | 256x256 | Snake glow during blast |
| `Sprites/Effects/eat_particle.png` | 32x32 | Eat sparkle particle |
| `Sprites/Effects/crown_burst.png` | 256x256 | Crown award celebration |
| `Sprites/Effects/blast_countdown_bg.png` | 400x400 | Countdown circle overlay |

**Total: 21 assets**
