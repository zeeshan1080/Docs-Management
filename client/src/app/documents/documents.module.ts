import { NgModule } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import {
  Check,
  ChevronLeft,
  ChevronRight,
  Download,
  EllipsisVertical,
  Eye,
  Folder,
  Folders,
  Info,
  Link2,
  Pencil,
  Plus,
  Search,
  Share2,
  Trash2,
  Upload,
  UserPlus,
  X,
} from 'lucide-angular';
import { SharedModule } from '../shared/shared.module';
import { DocumentsRoutingModule } from './documents-routing.module';
import { DocumentsComponent } from './documents.component';
import { FolderTreeComponent } from './folder-tree.component';

@NgModule({
  declarations: [DocumentsComponent, FolderTreeComponent],
  imports: [
    SharedModule,
    DocumentsRoutingModule,
    LucideAngularModule.pick({
      Check,
      ChevronLeft,
      ChevronRight,
      Download,
      EllipsisVertical,
      Eye,
      Folder,
      Folders,
      Info,
      Link2,
      Pencil,
      Plus,
      Search,
      Share2,
      Trash2,
      Upload,
      UserPlus,
      X,
    }),
  ],
})
export class DocumentsModule {}
