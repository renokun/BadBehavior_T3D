$inBehaviorTreeEditor = false;

//==============================================================================
// INIT
//==============================================================================
function BTEdit( %val )
{
   if(%val != 0)
      return;

   if (!$InBehaviorTreeEditor)
   {
      if(!isObject(BTEditCanvas))
         new GuiControl(BTEditCanvas, EditorGuiGroup);
      
      BTEditor.startUp(Canvas.getContent());
      
      $InBehaviorTreeEditor = true;
   }
   else
   {
      BTEditCanvas.quit();
   }
}

function toggleBehaviorTreeEditor( %make )
{
   if( %make )
   {
      //if( EditorIsActive() && !GuiEditor.toggleIntoEditorGui )
      //   toggleEditor( true );
         
      BTEdit();
      
	  // Cancel the scheduled event to prevent
	  // the level from cycling after it's duration
	  // has elapsed.
      cancel($Game::Schedule);
   }
}

GlobalActionMap.bind( keyboard, "f9", toggleBehaviorTreeEditor );


function BTEditor::startUp(%this, %content)
{
   %this.lastContent=%content;
   Canvas.setContent( BehaviorTreeEditorGui );
   
   if(BehaviorTreeGroup.getCount() > 0)
   {
      %this.open(BehaviorTreeGroup.getObject(0));
      %this.expandAll();
   }
   else
   {
      %tree = new Root(NewTree);
      BehaviorTreeGroup.add(%tree);
      %this.open(%tree);
   }
   
   %this.updateUndoMenu();
}


function BTEditor::refresh(%this)
{
   %root = %this.getItemValue(%this.getFirstRootItem());
   %this.open(%root);
   %this.expandAll();
}

//==============================================================================
// VIEW
//==============================================================================
function BTEditor::expandAll(%this)
{
   for(%i=1; %i<=%this.getItemCount(); %i++)
   {
      %this.expandItem(%i);
      %this.buildVisibleTree();
   }
}

function BTEditor::collapseAll(%this)
{
   for(%i=1; %i<=%this.getItemCount(); %i++)
      %this.expandItem(%i, false);
   %this.buildVisibleTree();
}

//==============================================================================
// REPARENT
//==============================================================================
function BTEditor::onBeginReparenting(%this)
{
   if( isObject( %this.reparentUndoAction ) )
      %this.reparentUndoAction.delete();
      
   %action = BTReparentUndoAction::create( %this );
   %this.reparentUndoAction = %action;
}

function BTEditor::onReparent(%this, %item, %old, %new)
{
   if( !isObject(%this.reparentUndoAction) ||
       %this.reparentUndoAction.node != %item )
   {
      warn( "Reparenting undo is borked :(" );
      if(isObject(%this.reparentUndoAction))
      {
         %this.reparentUndoAction.delete();
         %this.reparentUndoAction="";
      }
   }
   else
   {       
      %this.reparentUndoAction.oldParent = %old;
      %this.reparentUndoAction.newParent = %new;
      %this.reparentUndoAction.newPosition = %new.getObjectIndex(%item);
   }
}

function BTEditor::onEndReparenting( %this )
{
   %action = %this.reparentUndoAction;
   %this.reparentUndoAction = "";
   
   // Check that the reparenting went as planned, and undo it right now if not
   if(%action.node.getGroup() != %action.newParent)
   {
      %action.undo();
      %action.delete();
   }
   else
   {
      %action.addToManager( %this.getUndoManager() );
      BTEditorStatusBar.print( "Moved node" );
   }
}


//==============================================================================
// SELECT
//==============================================================================
function BTEditor::onSelect(%this, %item)
{
   BehaviorTreeInspector.inspect(%item);
}

function BTEditor::onUnselect(%this, %item)
{

}

function BTEditor::canAdd(%this, %obj, %target)
{
   if(!isObject(%target))
      return false;
      
   if( !%target.isMemberOfClass( "SimGroup" ) )
      return false;
      
   return %target.acceptsAsChild(%obj);
}

function BTEditor::isValidDragTarget(%this, %id, %obj)
{
   %selObj = %this.getSelectedObject();
   
   if(!%selObj)
      return false;
   
   return %this.canAdd(%selObj, %obj);
}

//==============================================================================
// DELETE
//==============================================================================
// onDeleteSelection is called prior to deleting the selected object. 
function BTEditor::onDeleteSelection(%this)
{
   if(%this.getSelectedItem() > 1) // not root
      BTDeleteUndoAction::submit(%this.getSelectedObject());
   
   %this.clearSelection();
   
   BTEditorStatusBar.print( "Node deleted" );
}

//==============================================================================
// UNDO
//==============================================================================
function BTEditor::getUndoManager( %this )
{
   if( !isObject( BehaviorTreeEditorUndoManager ) )
      new UndoManager( BehaviorTreeEditorUndoManager );
   
   return BehaviorTreeEditorUndoManager;
}

function BTEditor::updateUndoMenu(%this)
{
   %uman = %this.getUndoManager();
   %nextUndo = %uman.getNextUndoName();
   %nextRedo = %uman.getNextRedoName();
   
   %editMenu = BTEditCanvas.menuBar->editMenu;
   
   %editMenu.setItemName( 0, "Undo " @ %nextUndo );
   %editMenu.setItemName( 1, "Redo " @ %nextRedo );
   
   %editMenu.enableItem( 0, %nextUndo !$= "" );
   %editMenu.enableItem( 1, %nextRedo !$= "" );
}

function BTEditor::undo(%this)
{
   %action = %this.getUndoManager().getNextUndoName();
   
   %this.getUndoManager().undo();
   %this.updateUndoMenu();
   
   BTEditorStatusBar.print( "Undid '" @ %action @ "'" );
}

function BTEditor::redo(%this)
{
   %action = %this.getUndoManager().getNextRedoName();

   %this.getUndoManager().redo();
   %this.updateUndoMenu();
   
   BTEditorStatusBar.print( "Redid '" @ %action @ "'" );
}