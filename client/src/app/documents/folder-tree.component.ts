import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FolderNode } from '../models/models';

@Component({
  selector: 'app-folder-tree',
  templateUrl: './folder-tree.component.html',
})
export class FolderTreeComponent {
  @Input() nodes: FolderNode[] = [];
  @Input() selectedId: number | null = null;
  @Input() depth = 0;
  @Output() selectFolder = new EventEmitter<FolderNode>();

  pick(n: FolderNode): void {
    this.selectFolder.emit(n);
  }
}
