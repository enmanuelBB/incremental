using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

/// <summary>Valida los datos del árbol real de Alucard (el asset), para que un error de edición no llegue a la partida.</summary>
[TestFixture]
public class SkillTreeAssetTests
{
    private const string TreePath = "Assets/Data/Skills/Alucard_Tree.asset";
    private const string CharacterPath = "Assets/Data/Characters/Alucard.asset";

    private SkillTreeDefinition Tree => AssetDatabase.LoadAssetAtPath<SkillTreeDefinition>(TreePath);

    [Test]
    public void AlucardTree_Exists_AndIsLinkedFromTheCharacter()
    {
        Assert.IsNotNull(Tree, "Falta " + TreePath);

        var character = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(CharacterPath);
        Assert.AreSame(Tree, character.skillTree);
    }

    [Test]
    public void AlucardTree_NodeIdsAreUniqueAndNotEmpty()
    {
        List<string> ids = Tree.nodes.Select(n => n.id).ToList();

        Assert.IsTrue(ids.All(id => !string.IsNullOrEmpty(id)));
        Assert.AreEqual(ids.Count, ids.Distinct().Count());
    }

    [Test]
    public void AlucardTree_EveryConnectionPointsToAnExistingNode()
    {
        foreach (SkillNode node in Tree.nodes)
            foreach (string neighbour in node.connections)
                Assert.IsNotNull(Tree.Find(neighbour), node.id + " se conecta a '" + neighbour + "', que no existe");
    }

    [Test]
    public void AlucardTree_HasARoot_AndEveryNodeCanBeReachedFromARoot()
    {
        SkillNode[] nodes = Tree.nodes;
        Assert.IsTrue(nodes.Any(n => n.isRoot), "No hay ningún nodo raíz");

        // Compra en orden: repite hasta que no se desbloquee nada nuevo, igual que lo haría un jugador con puntos de sobra.
        var owned = new List<string>();
        bool progress = true;
        while (progress)
        {
            progress = false;
            foreach (SkillNode node in nodes)
            {
                if (owned.Contains(node.id) || !SkillTreeRules.IsUnlocked(Tree, node, owned)) continue;
                owned.Add(node.id);
                progress = true;
            }
        }

        var unreachable = nodes.Select(n => n.id).Except(owned).ToList();
        Assert.IsEmpty(unreachable, "Nodos inalcanzables: " + string.Join(", ", unreachable));
    }

    [Test]
    public void AlucardTree_EveryNodeHasANameAnEffectAndAPositiveCost()
    {
        foreach (SkillNode node in Tree.nodes)
        {
            Assert.IsFalse(string.IsNullOrEmpty(node.displayName), node.id + " sin nombre");
            Assert.IsNotEmpty(node.effects, node.id + " sin efectos");
            Assert.Greater(node.cost, 0, node.id + " debe costar al menos 1");
            foreach (SkillEffect effect in node.effects) Assert.Greater(effect.value, 0f, node.id + " con un efecto sin valor");
        }
    }

    // Ampliación: el árbol creció (más nodos de stats y una segunda ronda de mejoras de Q, E y F).
    [TestCase("q5")] [TestCase("q6")] [TestCase("q7")]
    [TestCase("e5")] [TestCase("e6")] [TestCase("e7")] [TestCase("e8")]
    [TestCase("f4")] [TestCase("f5")] [TestCase("f6")]
    [TestCase("v3")] [TestCase("v4")] [TestCase("s3")] [TestCase("s4")] [TestCase("d4")] [TestCase("d5")]
    public void AlucardTree_HasTheExpansionNodes(string id)
    {
        Assert.IsNotNull(Tree.Find(id), "falta el nodo " + id);
    }

    [Test]
    public void AlucardTree_HasMoreThan30Nodes()
    {
        Assert.GreaterOrEqual(Tree.nodes.Length, 35);
    }

    [Test]
    public void AlucardTree_NoTwoNodesOverlapOnScreen()
    {
        // Mismo reparto que SkillTreeView (zona de 1800 x 640, nodos de 112 px, escala X e Y por separado).
        const float areaWidth = 1800f, areaHeight = 640f, nodeSize = 112f, maxScale = 190f, margin = 4f;

        var min = new UnityEngine.Vector2(float.MaxValue, float.MaxValue);
        var max = new UnityEngine.Vector2(float.MinValue, float.MinValue);
        foreach (SkillNode n in Tree.nodes) { min = UnityEngine.Vector2.Min(min, n.position); max = UnityEngine.Vector2.Max(max, n.position); }
        float scaleX = UnityEngine.Mathf.Min(maxScale, (areaWidth - nodeSize) / UnityEngine.Mathf.Max(0.01f, max.x - min.x));
        float scaleY = UnityEngine.Mathf.Min(maxScale, (areaHeight - nodeSize) / UnityEngine.Mathf.Max(0.01f, max.y - min.y));

        for (int i = 0; i < Tree.nodes.Length; i++)
        {
            for (int j = i + 1; j < Tree.nodes.Length; j++)
            {
                float dx = UnityEngine.Mathf.Abs(Tree.nodes[i].position.x - Tree.nodes[j].position.x) * scaleX;
                float dy = UnityEngine.Mathf.Abs(Tree.nodes[i].position.y - Tree.nodes[j].position.y) * scaleY;
                Assert.IsTrue(dx >= nodeSize + margin || dy >= nodeSize + margin,
                    "los nodos " + Tree.nodes[i].id + " y " + Tree.nodes[j].id + " se pisan en pantalla (dx=" + dx.ToString("0") + ", dy=" + dy.ToString("0") + ")");
            }
        }
    }

    [Test]
    public void AlucardTree_CoversEveryAbilityEffectType()
    {
        var present = Tree.nodes.SelectMany(n => n.effects).Select(e => e.type).Distinct().ToList();

        // Los efectos de Guts (desde StunChance en adelante) son de su árbol, no del de Alucard.
        foreach (SkillEffectType type in System.Enum.GetValues(typeof(SkillEffectType)))
        {
            if (type >= SkillEffectType.StunChance) continue;
            Assert.Contains(type, present, "El árbol de Alucard no usa " + type);
        }
    }
}
