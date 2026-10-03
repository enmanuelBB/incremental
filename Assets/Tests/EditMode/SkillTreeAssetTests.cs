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

    [Test]
    public void AlucardTree_CoversEveryAbilityEffectType()
    {
        var present = Tree.nodes.SelectMany(n => n.effects).Select(e => e.type).Distinct().ToList();

        foreach (SkillEffectType type in System.Enum.GetValues(typeof(SkillEffectType)))
            Assert.Contains(type, present, "El árbol de Alucard no usa " + type);
    }
}
