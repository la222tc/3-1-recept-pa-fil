using FiledRecipes.Domain;
using FiledRecipes.App.Mvp;
using FiledRecipes.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FiledRecipes.Views
{
    /// <summary>
    /// 
    /// </summary>
    public class RecipeView : ViewBase, IRecipeView
    {

        public void Show(IRecipe recipe)
        {
            // Visar headerpanalen med receptnamnet
            Header = recipe.Name; 
            ShowHeaderPanel();

            // Skriver ut ingredienserna rad för rad
            Console.WriteLine();
            Console.WriteLine("--- Ingredienser ---\n");
            foreach (Ingredient ingredients in recipe.Ingredients)
            {
                Console.WriteLine(ingredients);
            }


            // Skriver ut instruktionerna rad för rad
            Console.WriteLine();
            Console.WriteLine("--- Instruktioner ---\n");
            foreach (string instructions in recipe.Instructions)
            {
                Console.WriteLine(instructions);
            }
        }

        public void Show(IEnumerable<IRecipe> recipes)
        {
            // Skriver ut alla recept 
            foreach (Recipe recipe in recipes)
            {
                Show(recipe);
                ContinueOnKeyPressed();
            }
        }
    }
}
