from flask import Flask, request, jsonify
from flask_cors import CORS
import uuid
import random

app = Flask(__name__)
CORS(app)

# =========================
# GAME STATE
# =========================

games = {}

# =========================
# MODELS
# =========================

class Stats:
    def __init__(self, hp, attack, defense, magic):
        self.hp = hp
        self.attack = attack
        self.defense = defense
        self.magic = magic


class Move:
    def __init__(self, name, effects):
        self.name = name
        self.effects = effects


class StatusEffect:
    def __init__(self, stat, value, duration):
        self.stat = stat
        self.value = value
        self.duration = duration


class Character:
    def __init__(self, name, stats, moves):
        self.name = name
        self.stats = stats
        self.moves = moves
        self.current_hp = stats.hp
        self.statuses = []  # active buffs/debuffs

    def get_modified_stat(self, stat):
        base = getattr(self.stats, stat)
        mod = sum(e.value for e in self.statuses if e.stat == stat)
        return base + mod

# =========================
# EFFECT ENGINE
# =========================

def apply_effect(caster, target, effect):
    t = effect["type"]
    stat = effect.get("stat")
    value = effect.get("value", 0)
    duration = effect.get("duration", 0)

    actual = caster if effect.get("target") == "self" else target

    # DAMAGE
    if t == "damage":
        if stat == "attack":
            dmg = caster.get_modified_stat("attack") * value - target.get_modified_stat("defense")
        elif stat == "magic":
            dmg = caster.get_modified_stat("magic") * value
        else:
            dmg = value

        dmg = int(dmg)

        # prevent negative damage
        dmg = max(0, dmg)

        actual.current_hp = max(0, actual.current_hp - dmg)

        return {"type": "damage", "target": actual.name, "value": int(dmg)}

    # HEAL
    if t == "heal":
        heal = caster.get_modified_stat("magic") * value
        actual.current_hp = min(actual.stats.hp, actual.current_hp + int(heal))

        return {"type": "heal", "target": actual.name, "value": int(heal)}

    # BUFF / DEBUFF
    if t in ["buff", "debuff"]:
        mod = value if t == "buff" else -value

        actual.statuses.append(StatusEffect(stat, mod, duration))

        return {
            "type": t,
            "target": actual.name,
            "stat": stat,
            "value": mod,
            "duration": duration
        }

    # SELF DAMAGE (Dark Pact etc.)
    if t == "self_damage":
        caster.current_hp = max(0, caster.current_hp - value)

        return {
            "type": "self_damage",
            "caster": caster.name,
            "value": value
        }

    return {"error": "unknown effect"}


def execute_move(caster, target, move):
    log = []

    for e in move.effects:
        log.append(apply_effect(caster, target, e))

    return log

# =========================
# STATUS TICK SYSTEM
# =========================

def tick_statuses(char):
    new = []

    for s in char.statuses:
        s.duration -= 1
        if s.duration > 0:
            new.append(s)

    char.statuses = new

# =========================
# TURN ENGINE
# =========================

def resolve_turn(game, monster, player_move):
    hero = game["hero"]
    log = []

    # PLAYER TURN
    log += execute_move(hero, monster, player_move)

    # Monster dies before acting
    if monster.current_hp <= 0:
        monster.current_hp = monster.stats.hp
        monster.statuses = []
        hero.current_hp = hero.stats.hp
        hero.statuses = []
        return {
            "battle_over": True,
            "winner": "player",

            "hero_hp": hero.current_hp,
            "monster_hp": monster.current_hp,

            "hero_stats": {
                "attack": hero.get_modified_stat("attack"),
                "defense": hero.get_modified_stat("defense"),
                "magic": hero.get_modified_stat("magic")
            },

            "monster_stats": {
                "attack": monster.get_modified_stat("attack"),
                "defense": monster.get_modified_stat("defense"),
                "magic": monster.get_modified_stat("magic")
            },

            "log": log
        }

    # MONSTER TURN
    monster_move = random.choice(monster.moves)
    log += execute_move(monster, hero, monster_move)

    # tick status durations
    tick_statuses(hero)
    tick_statuses(monster)

    # Hero dies after monster attack
    if hero.current_hp <= 0:
        hero.current_hp = hero.stats.hp
        hero.statuses = []
        monster.current_hp = monster.stats.hp
        monster.statuses = []
        return {
            "battle_over": True,
            "winner": "monster",

            "hero_hp": hero.current_hp,
            "monster_hp": monster.current_hp,

            "hero_stats": {
                "attack": hero.get_modified_stat("attack"),
                "defense": hero.get_modified_stat("defense"),
                "magic": hero.get_modified_stat("magic")
            },

            "monster_stats": {
                "attack": monster.get_modified_stat("attack"),
                "defense": monster.get_modified_stat("defense"),
                "magic": monster.get_modified_stat("magic")
            },

            "monster_move": monster_move.name,
            "log": log
        }

    # Normal turn result
    return {
        "battle_over": False,

        "hero_hp": hero.current_hp,
        "monster_hp": monster.current_hp,

        "hero_stats": {
            "attack": hero.get_modified_stat("attack"),
            "defense": hero.get_modified_stat("defense"),
            "magic": hero.get_modified_stat("magic")
        },

        "monster_stats": {
            "attack": monster.get_modified_stat("attack"),
            "defense": monster.get_modified_stat("defense"),
            "magic": monster.get_modified_stat("magic")
        },

        "monster_move": monster_move.name,
        "log": log
    }

def pick_reward_move(monster, known_moves):
    available = []

    for move in monster.moves:
        if move.name not in known_moves:
            available.append(move)

    if len(available) == 0:
        return None

    return random.choice(available)

# =========================
# CONFIG (MOVES DEFINED HERE)
# =========================

def scale_monster(monster, level):
    hp_scale = 15 * (level - 1)
    atk_scale = 3 * (level - 1)
    def_scale = 2 * (level - 1)
    magic_scale = 3 * (level - 1)

    monster.stats.hp += hp_scale
    monster.stats.attack += atk_scale
    monster.stats.defense += def_scale
    monster.stats.magic += magic_scale

    monster.current_hp = monster.stats.hp

    return monster

def create_game():
    hero = Character(
        "Knight",
        Stats(120, 15, 8, 10),
        [
            Move("Slash", [
                {"type": "damage", "target": "enemy", "stat": "attack", "value": 1.2}
            ]),
            Move("Shield Up", [
                {"type": "buff", "target": "self", "stat": "defense", "value": 5, "duration": 3}
            ]),
            Move("Battle Cry", [
                {"type": "buff", "target": "self", "stat": "attack", "value": 5, "duration": 3}
            ]),
            Move("Second Wind", [
                {"type": "heal", "target": "self", "stat": "magic", "value": 2}
            ])
        ]
    )

    base_monsters = [
        Character("Witch", Stats(80, 5, 4, 15), [
            Move("Shadow Bolt", [{"type": "damage", "target": "enemy", "stat": "magic", "value": 1.6}]),
            Move("Drain Life", [
                {"type": "damage", "target": "enemy", "stat": "magic", "value": 1.0},
                {"type": "heal", "target": "self", "stat": "magic", "value": 1.0}
            ]),
            Move("Curse", [
                {"type": "debuff", "target": "enemy", "stat": "attack", "value": 3, "duration": 3}
            ]),
            Move("Dark Pact", [
                {"type": "self_damage", "value": 10},
                {"type": "buff", "target": "self", "stat": "magic", "value": 5, "duration": 3}
            ])
        ]),

        Character("Giant Spider", Stats(90, 12, 6, 3), [
            Move("Bite", [{"type": "damage", "target": "enemy", "stat": "attack", "value": 1.2}]),
            Move("Web Throw", [
                {"type": "damage", "target": "enemy", "stat": "attack", "value": 0.8},
                {"type": "debuff", "target": "enemy", "stat": "defense", "value": 3, "duration": 3}
            ]),
            Move("Pounce", [{"type": "damage", "target": "enemy", "stat": "attack", "value": 1.8}]),
            Move("Skitter", [
                {"type": "buff", "target": "self", "stat": "defense", "value": 4, "duration": 3}
            ])
        ]),

        Character("Dragon", Stats(130, 16, 8, 14), [
            Move("Flame Breath", [{"type": "damage", "target": "enemy", "stat": "magic", "value": 2.0}]),
            Move("Claw Swipe", [{"type": "damage", "target": "enemy", "stat": "attack", "value": 1.3}]),
            Move("Intimidate", [
                {"type": "debuff", "target": "enemy", "stat": "attack", "value": 5, "duration": 3}
            ]),
            Move("Dragon Scales", [
                {"type": "buff", "target": "self", "stat": "defense", "value": 6, "duration": 3}
            ])
        ]),

        Character("Goblin Warrior", Stats(70, 14, 5, 2), [
            Move("Rusty Blade", [{"type": "damage", "target": "enemy", "stat": "attack", "value": 1.1}]),
            Move("Dirty Kick", [
                {"type": "damage", "target": "enemy", "stat": "attack", "value": 0.7},
                {"type": "debuff", "target": "enemy", "stat": "defense", "value": 2, "duration": 3}
            ]),
            Move("Frenzy", [
                {"type": "buff", "target": "self", "stat": "attack", "value": 4, "duration": 3}
            ]),
            Move("Headbutt", [{"type": "damage", "target": "enemy", "stat": "attack", "value": 1.5}])
        ]),

        Character("Goblin Mage", Stats(60, 4, 3, 14), [
            Move("Firebolt", [{"type": "damage", "target": "enemy", "stat": "magic", "value": 1.3}]),
            Move("Arcane Surge", [
                {"type": "buff", "target": "self", "stat": "magic", "value": 5, "duration": 3}
            ]),
            Move("Mana Drain", [
                {"type": "damage", "target": "enemy", "stat": "magic", "value": 0.8},
                {"type": "debuff", "target": "enemy", "stat": "magic", "value": 2, "duration": 3}
            ]),
            Move("Hex Shield", [
                {"type": "buff", "target": "self", "stat": "defense", "value": 3, "duration": 3}
            ])
        ])
    ]

    random.shuffle(base_monsters)

    scaled_monsters = []

    for i, monster in enumerate(base_monsters):
        level = i + 1
        scaled_monsters.append(scale_monster(monster, level))

    return hero, scaled_monsters

def get_all_moves():
    hero, monsters = create_game()

    all_moves = {}

    for move in hero.moves:
        all_moves[move.name] = move

    for monster in monsters:
        for move in monster.moves:
            all_moves[move.name] = move

    return all_moves

# =========================
# ENDPOINTS
# =========================

@app.route("/config", methods=["GET"])
def config():
    run_id = str(uuid.uuid4())

    hero, monsters = create_game()

    games[run_id] = {
        "hero": hero,
        "monsters": monsters
    }

    return jsonify({
        "run_id": run_id,

        "hero": {
            "name": hero.name,
            "stats": hero.stats.__dict__,
            "moves": [
                {
                    "name": m.name,
                    "effects": m.effects
                }
                for m in hero.moves
            ]
        },

        "monsters": [
            {
                "name": m.name,
                "stats": m.stats.__dict__,
                "moves": [
                    {
                        "name": mv.name,
                        "effects": mv.effects
                    }
                    for mv in m.moves
                ]
            }
            for m in monsters
        ]
    })

@app.route("/turn", methods=["POST"])
def turn():
    data = request.json

    game = games.get(data["run_id"])
    if not game:
        return jsonify({"error": "invalid run"}), 400

    monster_index = data["monster_index"]
    monster = game["monsters"][monster_index]

    equipped_move_names = data.get("equipped_moves", [])

    hero = game["hero"]

    all_moves = get_all_moves()

    equipped_moves = []

    for move_name in equipped_move_names:
        if move_name in all_moves:
            equipped_moves.append(all_moves[move_name])

    player_move_index = data["player_move_index"]

    if player_move_index >= len(equipped_moves):
        return jsonify({"error": "invalid move index"}), 400

    selected_move = equipped_moves[player_move_index]

    result = resolve_turn(game, monster, selected_move)

    known_moves = data.get("known_moves", [])

    reward = None

    if result.get("battle_over") and result.get("winner") == "player":
        reward_move = pick_reward_move(monster, known_moves)

        if reward_move:
            reward = {
                "name": reward_move.name,
                "effects": reward_move.effects
            }

    result["reward_move"] = reward

    return jsonify(result)

@app.route("/levelup", methods=["POST"])
def levelup():
    data = request.json

    game = games.get(data["run_id"])
    if not game:
        return jsonify({"error": "invalid run"}), 400

    hero = game["hero"]

    stat = data["stat"]     # "hp", "attack", "defense", "magic"

    if stat == "hp":
        hero.stats.hp += 10
    elif stat == "attack":
        hero.stats.attack += 1
    elif stat == "defense":
        hero.stats.defense += 1
    elif stat == "magic":
        hero.stats.magic += 1

    hero.current_hp = hero.stats.hp

    return jsonify({
    "hp": hero.stats.hp,
    "attack": hero.stats.attack,
    "defense": hero.stats.defense,
    "magic": hero.stats.magic
})

# =========================

if __name__ == "__main__":
    app.run(debug=True)
